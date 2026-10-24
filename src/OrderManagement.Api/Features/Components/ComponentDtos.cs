namespace OrderManagement.Api.Features.Components;

public sealed record CreateComponentRequest(string Name, string? Description, int Quantity);

public sealed record ComponentResponse(int Id, string Name, string Description, int Quantity)
{
    public static ComponentResponse From(Component component) =>
        new(component.Id, component.Name, component.Description, component.Quantity);
}
