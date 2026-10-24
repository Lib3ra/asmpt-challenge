using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Features.Components;

namespace OrderManagement.Api.Data;

/// <summary>EF Core context. The mapping of all entities is configured centrally in <see cref="OnModelCreating"/>.</summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Component> Components => Set<Component>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Component>(component =>
        {
            component.ToTable("Components");
            component.Property(c => c.Name).IsRequired();
            component.Property(c => c.Description).IsRequired();
            component.HasData(SeedData.Components);
        });
    }
}
