using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Data;
using OrderManagement.Api.Features.Boards;

namespace OrderManagement.Api.Features.Orders;

public sealed class OrderRepository(AppDbContext db, ILogger<OrderRepository> logger) : IOrderRepository
{
    public async Task<IReadOnlyList<Order>> SearchOrdersAsync(string? name, CancellationToken cancellationToken)
    {
        IQueryable<Order> query = db.Orders.AsNoTracking().Include(o => o.Boards);
        if (!string.IsNullOrWhiteSpace(name))
        {
            // Translates to instr(lower(Name), @term) > 0. SQLite's lower() folds ASCII letters only.
            string term = name.Trim().ToLowerInvariant();
            query = query.Where(o => o.Name.ToLower().Contains(term));
        }

        // DateOnly is stored as yyyy-MM-dd text, which sorts chronologically.
        return await query.OrderByDescending(o => o.OrderDate).ThenBy(o => o.Name).ToListAsync(cancellationToken);
    }

    public Task<Order?> GetOrderByIdAsync(int id, CancellationToken cancellationToken) =>
        db.Orders.AsNoTracking().Include(o => o.Boards).FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken cancellationToken) =>
        // The Name column uses the NOCASE collation, so = compares case-insensitively.
        db.Orders.AnyAsync(o => o.Name == name && o.Id != excludeId, cancellationToken);

    public async Task<IReadOnlyList<int>> FindMissingOrderIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        List<int> existingIds = await db.Orders.Where(o => ids.Contains(o.Id)).Select(o => o.Id).ToListAsync(cancellationToken);
        return ids.Except(existingIds).ToList();
    }

    public async Task<Order> CreateOrderAsync(Order order, IReadOnlyCollection<int> boardIds, CancellationToken cancellationToken)
    {
        order.Boards = await LoadBoardsAsync(boardIds, cancellationToken);
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created order {OrderId} ({OrderName}) dated {OrderDate} with boards {BoardIds}",
            order.Id, order.Name, order.OrderDate, boardIds);
        return order;
    }

    public async Task<Order?> UpdateOrderAsync(Order changes, IReadOnlyCollection<int> boardIds, CancellationToken cancellationToken)
    {
        Order? order = await db.Orders.Include(o => o.Boards).FirstOrDefaultAsync(o => o.Id == changes.Id, cancellationToken);
        if (order is null)
        {
            return null;
        }

        order.Name = changes.Name;
        order.Description = changes.Description;
        order.OrderDate = changes.OrderDate;
        order.Boards = await LoadBoardsAsync(boardIds, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Updated order {OrderId} ({OrderName}) with boards {BoardIds}", order.Id, order.Name, boardIds);
        return order;
    }

    public async Task DeleteOrdersAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        // The database removes the board links through ON DELETE CASCADE on the join table.
        int deleted = await db.Orders.Where(o => ids.Contains(o.Id)).ExecuteDeleteAsync(cancellationToken);
        logger.LogInformation("Deleted {OrderCount} order(s): {OrderIds}", deleted, ids);
    }

    private Task<List<Board>> LoadBoardsAsync(IReadOnlyCollection<int> boardIds, CancellationToken cancellationToken) =>
        db.Boards.Where(b => boardIds.Contains(b.Id)).ToListAsync(cancellationToken);
}
