using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Data;
using OrderManagement.Api.Features.Components;

namespace OrderManagement.Api.Features.Boards;

public sealed class BoardRepository(AppDbContext db, ILogger<BoardRepository> logger) : IBoardRepository
{
    public async Task<IReadOnlyList<Board>> SearchBoardsAsync(string? name, CancellationToken cancellationToken)
    {
        IQueryable<Board> query = db.Boards.AsNoTracking().Include(b => b.Components);
        if (!string.IsNullOrWhiteSpace(name))
        {
            // Translates to instr(lower(Name), @term) > 0. SQLite's lower() folds ASCII letters only.
            string term = name.Trim().ToLowerInvariant();
            query = query.Where(b => b.Name.ToLower().Contains(term));
        }

        return await query.OrderBy(b => b.Name).ToListAsync(cancellationToken);
    }

    public Task<Board?> GetBoardByIdAsync(int id, CancellationToken cancellationToken) =>
        db.Boards.AsNoTracking().Include(b => b.Components).FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken cancellationToken) =>
        // The Name column uses the NOCASE collation, so = compares case-insensitively.
        db.Boards.AnyAsync(b => b.Name == name && b.Id != excludeId, cancellationToken);

    public async Task<IReadOnlyList<int>> FindMissingBoardIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        List<int> existingIds = await db.Boards.Where(b => ids.Contains(b.Id)).Select(b => b.Id).ToListAsync(cancellationToken);
        return ids.Except(existingIds).ToList();
    }

    public async Task<Board> CreateBoardAsync(
        Board board, IReadOnlyCollection<int> componentIds, CancellationToken cancellationToken)
    {
        board.Components = await LoadComponentsAsync(componentIds, cancellationToken);
        db.Boards.Add(board);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created board {BoardId} ({BoardName}) with components {ComponentIds}",
            board.Id, board.Name, componentIds);
        return board;
    }

    public async Task<Board?> UpdateBoardAsync(
        Board changes, IReadOnlyCollection<int> componentIds, CancellationToken cancellationToken)
    {
        Board? board = await db.Boards.Include(b => b.Components).FirstOrDefaultAsync(b => b.Id == changes.Id, cancellationToken);
        if (board is null)
        {
            return null;
        }

        board.Name = changes.Name;
        board.Description = changes.Description;
        board.LengthMm = changes.LengthMm;
        board.WidthMm = changes.WidthMm;
        board.Components = await LoadComponentsAsync(componentIds, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Updated board {BoardId} ({BoardName}) with components {ComponentIds}",
            board.Id, board.Name, componentIds);
        return board;
    }

    public async Task DeleteBoardsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        // The database removes the component links through ON DELETE CASCADE on the join table.
        int deleted = await db.Boards.Where(b => ids.Contains(b.Id)).ExecuteDeleteAsync(cancellationToken);
        logger.LogInformation("Deleted {BoardCount} board(s): {BoardIds}", deleted, ids);
    }

    private Task<List<Component>> LoadComponentsAsync(IReadOnlyCollection<int> componentIds, CancellationToken cancellationToken) =>
        db.Components.Where(c => componentIds.Contains(c.Id)).ToListAsync(cancellationToken);
}
