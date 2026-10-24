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

    private static async Task<Ok<List<ComponentResponse>>> GetAllAsync(
        IComponentRepository componentRepository, CancellationToken cancellationToken)
    {
        IReadOnlyList<Component> components = await componentRepository.GetAllAsync(cancellationToken);
        return TypedResults.Ok(components.Select(ComponentResponse.From).ToList());
    }

    private static async Task<Results<Ok<ComponentResponse>, NotFound>> GetByIdAsync(
        int id, IComponentRepository componentRepository, CancellationToken cancellationToken)
    {
        Component? component = await componentRepository.GetByIdAsync(id, cancellationToken);
        return component is null ? TypedResults.NotFound() : TypedResults.Ok(ComponentResponse.From(component));
    }

    private static async Task<Created<ComponentResponse>> CreateAsync(
        CreateComponentRequest request, IComponentRepository componentRepository, CancellationToken cancellationToken)
    {
        Component component = await componentRepository.AddAsync(
            new Component
            {
                Name = request.Name,
                Description = request.Description ?? string.Empty,
                Quantity = request.Quantity,
            },
            cancellationToken);
        return TypedResults.Created($"{Route}/{component.Id}", ComponentResponse.From(component));
    }
}
