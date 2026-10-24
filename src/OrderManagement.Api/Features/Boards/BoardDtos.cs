using OrderManagement.Api.Features.Components;

namespace OrderManagement.Api.Features.Boards;

/// <summary>Request body for creating and updating a board, including the ids of its components.</summary>
public sealed record BoardRequest(string? Name, string? Description, decimal LengthMm, decimal WidthMm, List<int>? ComponentIds)
{
    /// <summary>The name as it is stored: trimmed.</summary>
    public string NormalizedName => Name?.Trim() ?? string.Empty;

    /// <summary>The description as it is stored: trimmed, empty when missing.</summary>
    public string NormalizedDescription => Description?.Trim() ?? string.Empty;

    public IReadOnlyList<int> ComponentIdList => ComponentIds ?? [];

    public Board ToBoard(int id = 0) => new()
    {
        Id = id,
        Name = NormalizedName,
        Description = NormalizedDescription,
        LengthMm = LengthMm,
        WidthMm = WidthMm,
    };
}

public sealed record BoardResponse(
    int Id, string Name, string Description, decimal LengthMm, decimal WidthMm, List<ComponentResponse> Components)
{
    public static BoardResponse From(Board board) =>
        new(board.Id, board.Name, board.Description, board.LengthMm, board.WidthMm,
            board.Components.OrderBy(c => c.Name).Select(ComponentResponse.From).ToList());
}
