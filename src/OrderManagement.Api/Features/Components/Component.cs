using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OrderManagement.Api.Features.Components;

/// <summary>An electronic component that can be placed on boards.</summary>
public sealed class Component
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;

    /// <summary>Number of copies of this component placed on a board.</summary>
    public int Quantity { get; set; }
}

internal sealed class ComponentConfiguration : IEntityTypeConfiguration<Component>
{
    public void Configure(EntityTypeBuilder<Component> builder)
    {
        builder.ToTable("Components");
        builder.Property(c => c.Name).IsRequired();
        builder.Property(c => c.Description).IsRequired();
    }
}
