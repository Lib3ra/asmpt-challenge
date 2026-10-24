using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Data;

namespace OrderManagement.Api.Features.Components;

public sealed class ComponentRepository(AppDbContext db, ILogger<ComponentRepository> logger) : IComponentRepository
{
    public async Task<IReadOnlyList<Component>> SearchComponentsAsync(string? name, CancellationToken cancellationToken)
    {
        IQueryable<Component> query = db.Components.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(name))
        {
            // Translates to instr(lower(Name), @term) > 0. SQLite's lower() folds ASCII letters only.
            string term = name.Trim().ToLowerInvariant();
            query = query.Where(c => c.Name.ToLower().Contains(term));
        }

        return await query.OrderBy(c => c.Name).ToListAsync(cancellationToken);
    }

    public Task<Component?> GetComponentByIdAsync(int id, CancellationToken cancellationToken) =>
        db.Components.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlySet<string>> FindExistingNamesAsync(
        IReadOnlyCollection<string> names, int? excludeId, CancellationToken cancellationToken)
    {
        // The Name column uses the NOCASE collation, so IN compares case-insensitively.
        List<string> existing = await db.Components
            .Where(c => names.Contains(c.Name) && c.Id != excludeId)
            .Select(c => c.Name)
            .ToListAsync(cancellationToken);
        return existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task CreateComponentsAsync(IReadOnlyList<Component> components, CancellationToken cancellationToken)
    {
        db.Components.AddRange(components);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created {ComponentCount} component(s): {ComponentIds}",
            components.Count, components.Select(c => c.Id));
    }

    public async Task UpdateComponentAsync(Component component, CancellationToken cancellationToken)
    {
        db.Components.Update(component);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Updated component {ComponentId} ({ComponentName})", component.Id, component.Name);
    }

    public async Task<IReadOnlyList<int>> DeleteComponentsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        List<Component> components = await db.Components
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(cancellationToken);

        List<int> missingIds = ids.Except(components.Select(c => c.Id)).ToList();
        if (missingIds.Count > 0)
        {
            return missingIds;
        }

        db.Components.RemoveRange(components);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Deleted {ComponentCount} component(s): {ComponentIds}", components.Count, ids);
        return [];
    }
}
