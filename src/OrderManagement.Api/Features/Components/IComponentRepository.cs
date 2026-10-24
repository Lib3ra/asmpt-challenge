namespace OrderManagement.Api.Features.Components;

/// <summary>Data access for components. Keeps EF Core out of the endpoints.</summary>
public interface IComponentRepository
{
    Task<IReadOnlyList<Component>> GetAllAsync(CancellationToken cancellationToken);

    Task<Component?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<Component> AddAsync(Component component, CancellationToken cancellationToken);
}
