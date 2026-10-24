namespace OrderManagement.Api.Features.Orders;

/// <summary>Data access for orders and their board links. Keeps EF Core out of the endpoints.</summary>
public interface IOrderRepository
{
    /// <summary>
    /// All orders with their boards, newest order date first, optionally filtered by a case-insensitive
    /// "contains" match on the name.
    /// </summary>
    Task<IReadOnlyList<Order>> SearchOrdersAsync(string? name, CancellationToken cancellationToken);

    /// <summary>One order with its boards.</summary>
    Task<Order?> GetOrderByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>One order with its boards and their components, as handed to production.</summary>
    Task<Order?> GetOrderForProductionAsync(int id, CancellationToken cancellationToken);

    /// <summary>Whether an order other than <paramref name="excludeId"/> already uses <paramref name="name"/> (case-insensitive).</summary>
    Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken cancellationToken);

    /// <summary>Returns those of <paramref name="ids"/> that do not belong to a stored order.</summary>
    Task<IReadOnlyList<int>> FindMissingOrderIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);

    /// <summary>Stores the order linked to the given boards. The board ids must exist.</summary>
    Task<Order> CreateOrderAsync(Order order, IReadOnlyCollection<int> boardIds, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the order's fields and replaces its board links. Returns null if the order does not exist.
    /// The board ids must exist.
    /// </summary>
    Task<Order?> UpdateOrderAsync(Order changes, IReadOnlyCollection<int> boardIds, CancellationToken cancellationToken);

    /// <summary>Deletes the orders and their board links. Callers check existence first.</summary>
    Task DeleteOrdersAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);
}
