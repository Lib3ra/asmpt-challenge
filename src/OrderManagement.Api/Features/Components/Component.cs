namespace OrderManagement.Api.Features.Components;

/// <summary>An electronic component that can be placed on boards.</summary>
public sealed class Component
{
    public const int NameMaxLength = 256;
    public const int DescriptionMaxLength = 2000;
    public const int MinQuantity = 1;
    public const int MaxQuantity = 10_000;

    public int Id { get; set; }

    /// <summary>Unique per component, compared case-insensitively.</summary>
    public required string Name { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>Number of copies of this component placed on a board.</summary>
    public int Quantity { get; set; }
}
