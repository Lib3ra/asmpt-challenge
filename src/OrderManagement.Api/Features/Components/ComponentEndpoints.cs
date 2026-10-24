using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Data;

namespace OrderManagement.Api.Features.Components;

// Minimal persistence flow for PR 2. Full CRUD, search and validation follow in PR 3.
public static class ComponentEndpoints
{
    public const string Route = "/api/components";

    public static IEndpointRouteBuilder MapComponentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);
        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id:int}", GetByIdAsync);
        group.MapPost("/", CreateAsync);
        return app;
    }

    private static async Task<Ok<List<ComponentResponse>>> GetAllAsync(AppDbContext db, CancellationToken ct)
    {
        var components = await db.Components
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(c => new ComponentResponse(c.Id, c.Name, c.Description, c.Quantity))
            .ToListAsync(ct);
        return TypedResults.Ok(components);
    }

    private static async Task<Results<Ok<ComponentResponse>, NotFound>> GetByIdAsync(int id, AppDbContext db, CancellationToken ct)
    {
        var component = await db.Components.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        return component is null ? TypedResults.NotFound() : TypedResults.Ok(ComponentResponse.From(component));
    }

    private static async Task<Created<ComponentResponse>> CreateAsync(
        CreateComponentRequest request, AppDbContext db, ILoggerFactory loggerFactory, CancellationToken ct)
    {
        var component = new Component
        {
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            Quantity = request.Quantity,
        };
        db.Components.Add(component);
        await db.SaveChangesAsync(ct);

        loggerFactory.CreateLogger(typeof(ComponentEndpoints))
            .LogInformation("Created component {ComponentId} ({ComponentName})", component.Id, component.Name);
        return TypedResults.Created($"{Route}/{component.Id}", ComponentResponse.From(component));
    }
}
