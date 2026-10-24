using OrderManagement.Api.Features.Boards;

namespace OrderManagement.Api.Features.Orders;

/// <summary>Request body for creating and updating an order, including the ids of its boards.</summary>
public sealed record OrderRequest(string? Name, string? Description, DateOnly? OrderDate, List<int>? BoardIds)
{
    /// <summary>The name as it is stored: trimmed.</summary>
    public string NormalizedName => Name?.Trim() ?? string.Empty;

    /// <summary>The description as it is stored: trimmed, empty when missing.</summary>
    public string NormalizedDescription => Description?.Trim() ?? string.Empty;

    public IReadOnlyList<int> BoardIdList => BoardIds ?? [];

    /// <summary>Only called after validation, which guarantees an order date.</summary>
    public Order ToOrder(int id = 0) => new()
    {
        Id = id,
        Name = NormalizedName,
        Description = NormalizedDescription,
        OrderDate = OrderDate!.Value,
    };
}

/// <summary>A board as listed in an order: enough to recognize it. The full board is available under /api/boards.</summary>
public sealed record OrderBoardResponse(int Id, string Name)
{
    public static OrderBoardResponse From(Board board) => new(board.Id, board.Name);
}

public sealed record OrderResponse(int Id, string Name, string Description, DateOnly OrderDate, List<OrderBoardResponse> Boards)
{
    public static OrderResponse From(Order order) =>
        new(order.Id, order.Name, order.Description, order.OrderDate,
            order.Boards.OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase).Select(OrderBoardResponse.From).ToList());
}
