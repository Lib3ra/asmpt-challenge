using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Features.Components;

namespace OrderManagement.Api.Data;

/// <summary>
/// EF Core context. The mapping of all entities is configured centrally in <see cref="OnModelCreating"/>.
/// Table names follow the EF Core convention (the DbSet property name, e.g. "Components").
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Component> Components => Set<Component>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // No extra index on Id: the int primary key becomes SQLite's INTEGER PRIMARY KEY, an alias for the
        // rowid. The table itself is a B-tree ordered by it, so lookups by Id are already index lookups.
        modelBuilder.Entity<Component>(component =>
        {
            // SQLite stores all text as TEXT and does not enforce lengths. The limits document the model,
            // carry over to other database providers, and are enforced by request validation (PR 3).
            component.Property(c => c.Name).IsRequired().HasMaxLength(Component.NameMaxLength);
            component.Property(c => c.Description).IsRequired().HasMaxLength(Component.DescriptionMaxLength);
            component.HasData(SeedData.Components);
        });
    }
}
