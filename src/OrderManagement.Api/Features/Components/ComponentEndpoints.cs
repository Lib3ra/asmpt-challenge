using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.Common.Validation;

namespace OrderManagement.Api.Features.Components;

public static class ComponentEndpoints
{
    public const string Route = "/api/components";

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
                + "errors are keyed by item, e.g. '[1].quantity'. Name: required, max 256, unique ignoring case. "
                + "Description: optional, trimmed, max 2000. Quantity: 1 to 10,000 copies per board.");
        group.MapPut("/{id:int}", UpdateAsync)
            .WithSummary("Update one component")
            .WithDescription("Same rules as create; the name must not belong to another component.");
        group.MapDelete("/", DeleteAsync)
            .WithSummary("Delete one or more components")
            .WithDescription("Pass the ids as '?ids=1&ids=2'. All-or-nothing: if any id is unknown, nothing is deleted.");
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
        List<ComponentRequest> requests, IComponentRepository componentRepository, CancellationToken cancellationToken)
    {
        // Step 1: rules that need no database. Invalid requests stop here without a database query.
        ValidationErrors errors = ComponentValidator.ValidateAll(requests);
        if (errors.HasErrors)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        // Step 2: names must not belong to stored components.
        IReadOnlySet<string> existingNames = await componentRepository.FindExistingNamesAsync(
            requests.Select(request => request.NormalizedName).ToList(), excludeId: null, cancellationToken);
        for (int index = 0; index < requests.Count; index++)
        {
            AddErrorIfNameExists(requests[index].NormalizedName, existingNames, $"[{index}].{EntityRules.NameField}", errors);
        }

        if (errors.HasErrors)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        List<Component> components = requests.Select(request => request.ToComponent()).ToList();
        await componentRepository.CreateComponentsAsync(components, cancellationToken);
        return TypedResults.Created((string?)null, components.Select(ComponentResponse.From).ToList());
    }

    private static async Task<Results<Ok<ComponentResponse>, NotFound, ValidationProblem>> UpdateAsync(
        int id, ComponentRequest request, IComponentRepository componentRepository, CancellationToken cancellationToken)
    {
        if (await componentRepository.GetComponentByIdAsync(id, cancellationToken) is null)
        {
            return TypedResults.NotFound();
        }

        // Step 1: rules that need no database. Invalid requests stop here without a database query.
        ValidationErrors errors = ComponentValidator.Validate(request);
        if (errors.HasErrors)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        // Step 2: the name must not belong to another stored component.
        IReadOnlySet<string> existingNames = await componentRepository.FindExistingNamesAsync(
            [request.NormalizedName], excludeId: id, cancellationToken);
        AddErrorIfNameExists(request.NormalizedName, existingNames, EntityRules.NameField, errors);
        if (errors.HasErrors)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        Component component = request.ToComponent(id);
        await componentRepository.UpdateComponentAsync(component, cancellationToken);
        return TypedResults.Ok(ComponentResponse.From(component));
    }

    /// <summary>Deletes one or more components (<c>?ids=1&amp;ids=2</c>). All-or-nothing: unknown ids delete nothing.</summary>
    private static async Task<Results<NoContent, NotFound<ProblemDetails>, ValidationProblem>> DeleteAsync(
        int[]? ids, IComponentRepository componentRepository, CancellationToken cancellationToken)
    {
        if (ids is null || ids.Length == 0)
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["ids"] = ["At least one id is required."] });
        }

        IReadOnlyList<int> missingIds = await componentRepository.DeleteComponentsAsync(ids.Distinct().ToList(), cancellationToken);
        if (missingIds.Count > 0)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Title = "Components not found",
                Detail = $"No component exists with id {string.Join(", ", missingIds)}. Nothing was deleted.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        return TypedResults.NoContent();
    }

    private static void AddErrorIfNameExists(
        string name, IReadOnlySet<string> existingNames, string field, ValidationErrors errors)
    {
        if (existingNames.Contains(name))
        {
            errors.Add(field, $"A component named '{name}' already exists.");
        }
    }
}
