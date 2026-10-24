using Microsoft.AspNetCore.Http.HttpResults;

namespace OrderManagement.Api.Features.Components;

// Minimal persistence flow for PR 2. Full CRUD, search and validation follow in PR 3.
public static class ComponentEndpoints
{
    public const string Route = "/api/components";

    public static IEndpointRouteBuilder MapComponentEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(Route);
        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id:int}", GetByIdAsync);
        group.MapPost("/", CreateAsync);
        return app;
    }

    private static async Task<Ok<List<ComponentResponse>>> GetAllAsync(IComponentRepository repository, CancellationToken ct)
    {
        IReadOnlyList<Component> components = await repository.GetAllAsync(ct);
        return TypedResults.Ok(components.Select(ComponentResponse.From).ToList());
    }

    private static async Task<Results<Ok<ComponentResponse>, NotFound>> GetByIdAsync(
        int id, IComponentRepository repository, CancellationToken ct)
    {
        Component? component = await repository.GetByIdAsync(id, ct);
        return component is null ? TypedResults.NotFound() : TypedResults.Ok(ComponentResponse.From(component));
    }

    private static async Task<Created<ComponentResponse>> CreateAsync(
        CreateComponentRequest request, IComponentRepository repository, CancellationToken ct)
    {
        Component component = await repository.AddAsync(
            new Component
            {
                Name = request.Name,
                Description = request.Description ?? string.Empty,
                Quantity = request.Quantity,
            },
            ct);
        return TypedResults.Created($"{Route}/{component.Id}", ComponentResponse.From(component));
    }
}
