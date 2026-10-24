namespace OrderManagement.Api.Features.Components;

/// <summary>Data access for components. Keeps EF Core out of the endpoints.</summary>
public interface IComponentRepository
{
    /// <summary>All components, optionally filtered by a case-insensitive "contains" match on the name.</summary>
    Task<IReadOnlyList<Component>> SearchAsync(string? name, CancellationToken cancellationToken);

    Task<Component?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns those of <paramref name="names"/> that already belong to a stored component (case-insensitive),
    /// ignoring the component with <paramref name="excludeId"/>.
    /// </summary>
    Task<IReadOnlySet<string>> FindExistingNamesAsync(
        IReadOnlyCollection<string> names, int? excludeId, CancellationToken cancellationToken);

    /// <summary>Stores all components in one transaction.</summary>
    Task AddRangeAsync(IReadOnlyList<Component> components, CancellationToken cancellationToken);

    Task UpdateAsync(Component component, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes all components with the given ids in one transaction. If any id does not exist, nothing is
    /// deleted and the missing ids are returned.
    /// </summary>
    Task<IReadOnlyList<int>> DeleteRangeAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);
}
