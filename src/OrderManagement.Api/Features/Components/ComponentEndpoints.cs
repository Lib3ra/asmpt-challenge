using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.Common.Endpoints;
using OrderManagement.Api.Common.Validation;

namespace OrderManagement.Api.Features.Components;

public static class ComponentEndpoints
{
    public const string Route = "/api/components";
    private const string EntityName = "component";

    public static IEndpointRouteBuilder MapComponentEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(Route).WithTags("Components");
        group.MapGet("/", SearchAsync)
            .WithSummary("List components")
            .WithDescription("Ordered by name. The optional 'name' filter matches case-insensitively anywhere in the name.");
        group.MapGet("/{id:int}", GetByIdAsync)
            .WithSummary("Get one component");
        group.MapPost("/", CreateAsync)
            .WithSummary("Create one or more components")
            .WithDescription("Takes a JSON array. All-or-nothing: if any item is invalid, nothing is stored and the "
                + "errors are keyed by item, e.g. 'components[1].quantity'. Name: required, max 256, unique ignoring case. "
                + "Description: optional, trimmed, max 2000. Quantity: 1 to 10,000 copies per board.");
        group.MapPut("/{id:int}", UpdateAsync)
            .WithSummary("Update one component")
            .WithDescription("Same rules as create; the name must not belong to another component.");
        group.MapDelete("/", DeleteAsync)
            .WithSummary("Delete one or more components")
            .WithDescription("Pass the ids as '?ids=1&ids=2'. All-or-nothing: if any id is unknown (404) or any "
                + "component is still placed on a board (409), nothing is deleted.");
        return app;
    }

    private static async Task<Ok<List<ComponentResponse>>> SearchAsync(
        string? name, IComponentRepository componentRepository, CancellationToken cancellationToken)
    {
        IReadOnlyList<Component> components = await componentRepository.SearchComponentsAsync(name, cancellationToken);
        return TypedResults.Ok(components.Select(ComponentResponse.From).ToList());
    }

    private static async Task<Results<Ok<ComponentResponse>, NotFound>> GetByIdAsync(
        int id, IComponentRepository componentRepository, CancellationToken cancellationToken)
    {
        Component? component = await componentRepository.GetComponentByIdAsync(id, cancellationToken);
        return component is null ? TypedResults.NotFound() : TypedResults.Ok(ComponentResponse.From(component));
    }

    /// <summary>Creates one or more components. All-or-nothing: if any item is invalid, nothing is stored.</summary>
    private static async Task<Results<Created<List<ComponentResponse>>, ValidationProblem>> CreateAsync(
        List<ComponentRequest> requests,
        IValidator<List<ComponentRequest>> createComponentsValidator,
        IComponentRepository componentRepository,
        CancellationToken cancellationToken)
    {
        // Step 1: rules that need no database. Invalid requests stop here without a database query.
        ValidationResult validation = await createComponentsValidator.ValidateAsync(requests, cancellationToken);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToProblemErrors());
        }

        // Step 2: names must not belong to stored components.
        IReadOnlySet<string> existingNames = await componentRepository.FindExistingNamesAsync(
            requests.Select(request => request.NormalizedName).ToList(), excludeId: null, cancellationToken);
        Dictionary<string, string[]> nameErrors = NameConflicts.ToErrors(
            EntityName,
            requests.Select((request, index) => ($"{CreateComponentsRequestValidator.ListName}[{index}].name", request.NormalizedName)),
            existingNames);
        if (nameErrors.Count > 0)
        {
            return TypedResults.ValidationProblem(nameErrors);
        }

        List<Component> components = requests.Select(request => request.ToComponent()).ToList();
        await componentRepository.CreateComponentsAsync(components, cancellationToken);
        return TypedResults.Created((string?)null, components.Select(ComponentResponse.From).ToList());
    }

    private static async Task<Results<Ok<ComponentResponse>, NotFound, ValidationProblem>> UpdateAsync(
        int id,
        ComponentRequest request,
        IValidator<ComponentRequest> componentValidator,
        IComponentRepository componentRepository,
        CancellationToken cancellationToken)
    {
        if (await componentRepository.GetComponentByIdAsync(id, cancellationToken) is null)
        {
            return TypedResults.NotFound();
        }

        // Step 1: rules that need no database. Invalid requests stop here without a database query.
        ValidationResult validation = await componentValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToProblemErrors());
        }

        // Step 2: the name must not belong to another stored component.
        IReadOnlySet<string> existingNames = await componentRepository.FindExistingNamesAsync(
            [request.NormalizedName], excludeId: id, cancellationToken);
        Dictionary<string, string[]> nameErrors = NameConflicts.ToErrors(
            EntityName, [("name", request.NormalizedName)], existingNames);
        if (nameErrors.Count > 0)
        {
            return TypedResults.ValidationProblem(nameErrors);
        }

        Component component = request.ToComponent(id);
        await componentRepository.UpdateComponentAsync(component, cancellationToken);
        return TypedResults.Ok(ComponentResponse.From(component));
    }

    /// <summary>
    /// Deletes one or more components (<c>?ids=1&amp;ids=2</c>). All-or-nothing: unknown ids (404) or components
    /// still placed on a board (409) delete nothing.
    /// </summary>
    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem>> DeleteAsync(
        int[]? ids, IComponentRepository componentRepository, CancellationToken cancellationToken)
    {
        if (ids is null || ids.Length == 0)
        {
            return ProblemResults.IdsRequired();
        }

        List<int> distinctIds = ids.Distinct().ToList();
        IReadOnlyList<int> missingIds = await componentRepository.FindMissingComponentIdsAsync(distinctIds, cancellationToken);
        if (missingIds.Count > 0)
        {
            return ProblemResults.IdsNotFound(EntityName, missingIds);
        }

        IReadOnlyList<ComponentUsage> usages = await componentRepository.FindBoardsUsingComponentsAsync(distinctIds, cancellationToken);
        if (usages.Count > 0)
        {
            return ProblemResults.InUse($"Still placed on boards: {DescribeUsages(usages)}.");
        }

        await componentRepository.DeleteComponentsAsync(distinctIds, cancellationToken);
        return TypedResults.NoContent();
    }

    /// <summary>e.g. "'LED green' on Controller board, Power board; 'Resistor 10 kOhm' on Controller board".</summary>
    private static string DescribeUsages(IEnumerable<ComponentUsage> usages) =>
        string.Join("; ", usages
            .GroupBy(usage => usage.ComponentName)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => $"'{group.Key}' on {string.Join(", ", group.Select(usage => usage.BoardName).Order(StringComparer.OrdinalIgnoreCase))}"));
}
