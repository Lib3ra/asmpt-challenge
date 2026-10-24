using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Data;

namespace OrderManagement.Api.Features.Components;

public sealed class ComponentRepository(AppDbContext db, ILogger<ComponentRepository> logger) : IComponentRepository
{
    public async Task<IReadOnlyList<Component>> GetAllAsync(CancellationToken cancellationToken) =>
        await db.Components.AsNoTracking().OrderBy(c => c.Id).ToListAsync(cancellationToken);

    public Task<Component?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        db.Components.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Component> AddAsync(Component component, CancellationToken cancellationToken)
    {
        db.Components.Add(component);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created component {ComponentId} ({ComponentName})", component.Id, component.Name);
        return component;
    }
}
