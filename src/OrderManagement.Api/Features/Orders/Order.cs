using OrderManagement.Api.Common.Validation;
using OrderManagement.Api.Features.Boards;

namespace OrderManagement.Api.Features.Orders;

/// <summary>A production order for one or more boards.</summary>
public sealed class Order
{
    public const int NameMaxLength = EntityLimits.NameMaxLength;
    public const int DescriptionMaxLength = EntityLimits.DescriptionMaxLength;

    public int Id { get; set; }

    /// <summary>Unique per order, compared case-insensitively.</summary>
    public required string Name { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>Date only; no time of day and no time zone.</summary>
    public DateOnly OrderDate { get; set; }

    /// <summary>At least one board. A board can be produced in many orders.</summary>
    public List<Board> Boards { get; set; } = [];
}
