using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Data;
using OrderManagement.Api.Features.Boards;

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

    public async Task<IReadOnlyList<int>> FindMissingComponentIdsAsync(
        IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        List<int> existingIds = await db.Components
            .Where(c => ids.Contains(c.Id))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
        return ids.Except(existingIds).ToList();
    }

    public async Task<IReadOnlyList<ComponentUsage>> FindBoardsUsingComponentsAsync(
        IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        // Loads only the affected boards; the pairs are built in memory because a correlated
        // SelectMany would need SQL APPLY, which SQLite does not support.
        List<Board> boards = await db.Boards
            .AsNoTracking()
            .Include(board => board.Components)
            .Where(board => board.Components.Any(component => ids.Contains(component.Id)))
            .ToListAsync(cancellationToken);

        return boards
            .SelectMany(board => board.Components
                .Where(component => ids.Contains(component.Id))
                .Select(component => new ComponentUsage(component.Name, board.Name)))
            .ToList();
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

    public async Task DeleteComponentsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        int deleted = await db.Components.Where(c => ids.Contains(c.Id)).ExecuteDeleteAsync(cancellationToken);
        logger.LogInformation("Deleted {ComponentCount} component(s): {ComponentIds}", deleted, ids);
    }
}
