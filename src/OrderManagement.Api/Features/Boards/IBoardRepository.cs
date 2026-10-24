namespace OrderManagement.Api.Features.Boards;

/// <summary>Data access for boards and their component links. Keeps EF Core out of the endpoints.</summary>
public interface IBoardRepository
{
    /// <summary>All boards with their components, optionally filtered by a case-insensitive "contains" match on the name.</summary>
    Task<IReadOnlyList<Board>> SearchBoardsAsync(string? name, CancellationToken cancellationToken);

    /// <summary>One board with its components.</summary>
    Task<Board?> GetBoardByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Whether a board other than <paramref name="excludeId"/> already uses <paramref name="name"/> (case-insensitive).</summary>
    Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken cancellationToken);

    /// <summary>Returns those of <paramref name="ids"/> that do not belong to a stored board.</summary>
    Task<IReadOnlyList<int>> FindMissingBoardIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);

    /// <summary>Stores the board linked to the given components. The component ids must exist.</summary>
    Task<Board> CreateBoardAsync(Board board, IReadOnlyCollection<int> componentIds, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the board's fields and replaces its component links. Returns null if the board does not exist.
    /// The component ids must exist.
    /// </summary>
    Task<Board?> UpdateBoardAsync(Board changes, IReadOnlyCollection<int> componentIds, CancellationToken cancellationToken);

    /// <summary>Deletes the boards and their component links. Callers check existence first.</summary>
    Task DeleteBoardsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);
}
