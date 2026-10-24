using OrderManagement.Api.Features.Boards;
using OrderManagement.Api.Features.Components;

namespace OrderManagement.Api.Features.Orders;

/// <summary>
/// The file handed to a production line: the order with everything needed to build it, nested
/// order → boards → components. Boards and components are sorted by name.
/// </summary>
public sealed record ProductionFile(DateTimeOffset ExportedAt, ProductionOrder Order)
{
    public static ProductionFile From(Order order, DateTimeOffset exportedAt) => new(exportedAt, ProductionOrder.From(order));
}

public sealed record ProductionOrder(int Id, string Name, string Description, DateOnly OrderDate, List<ProductionBoard> Boards)
{
    public static ProductionOrder From(Order order) =>
        new(order.Id, order.Name, order.Description, order.OrderDate,
            order.Boards.OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase).Select(ProductionBoard.From).ToList());
}

/// <summary>A board with its components; Quantity is the number of copies of the component on this board.</summary>
public sealed record ProductionBoard(
    int Id, string Name, string Description, decimal LengthMm, decimal WidthMm, List<ComponentResponse> Components)
{
    public static ProductionBoard From(Board board) =>
        new(board.Id, board.Name, board.Description, board.LengthMm, board.WidthMm,
            board.Components.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).Select(ComponentResponse.From).ToList());
}
