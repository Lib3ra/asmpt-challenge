using OrderManagement.Api.Common.Validation;
using OrderManagement.Api.Features.Components;

namespace OrderManagement.Api.Features.Boards;

/// <summary>A printed circuit board with the components placed on it.</summary>
public sealed class Board
{
    public const int NameMaxLength = EntityLimits.NameMaxLength;
    public const int DescriptionMaxLength = EntityLimits.DescriptionMaxLength;
    public const decimal MaxDimensionMm = 1000m;
    public const int DimensionDecimals = 2;

    public int Id { get; set; }

    /// <summary>Unique per board, compared case-insensitively.</summary>
    public required string Name { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal LengthMm { get; set; }

    public decimal WidthMm { get; set; }

    /// <summary>At least one component. A component can be placed on many boards.</summary>
    public List<Component> Components { get; set; } = [];
}
