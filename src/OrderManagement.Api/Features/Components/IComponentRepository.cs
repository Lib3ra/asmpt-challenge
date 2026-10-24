namespace OrderManagement.Api.Features.Components;

/// <summary>Data access for components. Keeps EF Core out of the endpoints.</summary>
public interface IComponentRepository
{
    /// <summary>All components, optionally filtered by a case-insensitive "contains" match on the name.</summary>
    Task<IReadOnlyList<Component>> SearchComponentsAsync(string? name, CancellationToken cancellationToken);

    Task<Component?> GetComponentByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns those of <paramref name="names"/> that already belong to a stored component (case-insensitive),
    /// ignoring the component with <paramref name="excludeId"/>.
    /// </summary>
    Task<IReadOnlySet<string>> FindExistingNamesAsync(
        IReadOnlyCollection<string> names, int? excludeId, CancellationToken cancellationToken);

    /// <summary>Returns those of <paramref name="ids"/> that do not belong to a stored component.</summary>
    Task<IReadOnlyList<int>> FindMissingComponentIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);

    /// <summary>Lists which of the given components are placed on which boards.</summary>
    Task<IReadOnlyList<ComponentUsage>> FindBoardsUsingComponentsAsync(
        IReadOnlyCollection<int> ids, CancellationToken cancellationToken);

    /// <summary>Stores all components in one transaction.</summary>
    Task CreateComponentsAsync(IReadOnlyList<Component> components, CancellationToken cancellationToken);

    Task UpdateComponentAsync(Component component, CancellationToken cancellationToken);

    /// <summary>Deletes the components with the given ids in one statement. Callers check existence and usage first.</summary>
    Task DeleteComponentsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);
}

/// <summary>One component placed on one board; used to explain why a delete is blocked.</summary>
public sealed record ComponentUsage(string ComponentName, string BoardName);
