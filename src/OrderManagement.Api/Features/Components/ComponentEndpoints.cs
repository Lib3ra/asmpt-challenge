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
                + "Description: optional, max 2000. Quantity: 1 to 10,000 copies per board.");
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
        IReadOnlyList<Component> components = await componentRepository.SearchAsync(name, cancellationToken);
        return TypedResults.Ok(components.Select(ComponentResponse.From).ToList());
    }

    private static async Task<Results<Ok<ComponentResponse>, NotFound>> GetByIdAsync(
        int id, IComponentRepository componentRepository, CancellationToken cancellationToken)
    {
        Component? component = await componentRepository.GetByIdAsync(id, cancellationToken);
        return component is null ? TypedResults.NotFound() : TypedResults.Ok(ComponentResponse.From(component));
    }

    /// <summary>Creates one or more components. All-or-nothing: if any item is invalid, nothing is stored.</summary>
    private static async Task<Results<Created<List<ComponentResponse>>, ValidationProblem>> CreateAsync(
        List<ComponentRequest> requests, IComponentRepository componentRepository, CancellationToken cancellationToken)
    {
        ValidationErrors errors = ComponentValidator.ValidateAll(requests);
        if (!errors.HasErrors)
        {
            IReadOnlySet<string> existingNames = await componentRepository.FindExistingNamesAsync(
                requests.Select(request => request.NormalizedName).ToList(), excludeId: null, cancellationToken);
            for (int index = 0; index < requests.Count; index++)
            {
                AddErrorIfNameExists(requests[index].NormalizedName, existingNames, $"[{index}].{EntityRules.NameField}", errors);
            }
        }

        if (errors.HasErrors)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        List<Component> components = requests.Select(request => request.ToComponent()).ToList();
        await componentRepository.AddRangeAsync(components, cancellationToken);
        return TypedResults.Created((string?)null, components.Select(ComponentResponse.From).ToList());
    }

    private static async Task<Results<Ok<ComponentResponse>, NotFound, ValidationProblem>> UpdateAsync(
        int id, ComponentRequest request, IComponentRepository componentRepository, CancellationToken cancellationToken)
    {
        if (await componentRepository.GetByIdAsync(id, cancellationToken) is null)
        {
            return TypedResults.NotFound();
        }

        ValidationErrors errors = ComponentValidator.Validate(request);
        if (!errors.HasErrors)
        {
            IReadOnlySet<string> existingNames = await componentRepository.FindExistingNamesAsync(
                [request.NormalizedName], excludeId: id, cancellationToken);
            AddErrorIfNameExists(request.NormalizedName, existingNames, EntityRules.NameField, errors);
        }

        if (errors.HasErrors)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        Component component = request.ToComponent(id);
        await componentRepository.UpdateAsync(component, cancellationToken);
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

        IReadOnlyList<int> missingIds = await componentRepository.DeleteRangeAsync(ids.Distinct().ToList(), cancellationToken);
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
