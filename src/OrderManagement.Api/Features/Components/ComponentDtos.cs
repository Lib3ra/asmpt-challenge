namespace OrderManagement.Api.Features.Components;

/// <summary>Request body for creating (as list items) and updating a component.</summary>
public sealed record ComponentRequest(string? Name, string? Description, int Quantity)
{
    /// <summary>The name as it is stored: trimmed.</summary>
    public string NormalizedName => Name?.Trim() ?? string.Empty;

    public Component ToComponent(int id = 0) => new()
    {
        Id = id,
        Name = NormalizedName,
        Description = Description ?? string.Empty,
        Quantity = Quantity,
    };
}

public sealed record ComponentResponse(int Id, string Name, string Description, int Quantity)
{
    public static ComponentResponse From(Component component) =>
        new(component.Id, component.Name, component.Description, component.Quantity);
}
