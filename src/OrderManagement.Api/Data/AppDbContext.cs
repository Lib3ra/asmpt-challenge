using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Features.Boards;
using OrderManagement.Api.Features.Components;

namespace OrderManagement.Api.Data;

/// <summary>
/// EF Core context. The mapping of all entities is configured centrally in <see cref="OnModelCreating"/>.
/// Table names follow the EF Core convention (the DbSet property name, e.g. "Components").
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Component> Components => Set<Component>();

    public DbSet<Board> Boards => Set<Board>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // No extra index on Id: the int primary key becomes SQLite's INTEGER PRIMARY KEY, an alias for the
        // rowid. The table itself is a B-tree ordered by it, so lookups by Id are already index lookups.
        modelBuilder.Entity<Component>(component =>
        {
            // SQLite stores all text as TEXT and does not enforce lengths. The limits document the model,
            // carry over to other database providers, and are enforced by request validation (FluentValidation validators).
            // NOCASE makes comparisons and the unique index case-insensitive (for ASCII letters, as SQLite does).
            component.Property(c => c.Name).IsRequired().HasMaxLength(Component.NameMaxLength).UseCollation("NOCASE");
            component.HasIndex(c => c.Name).IsUnique();
            component.Property(c => c.Description).IsRequired().HasMaxLength(Component.DescriptionMaxLength);
            component.HasData(SeedData.Components);
        });

        modelBuilder.Entity<Board>(board =>
        {
            board.Property(b => b.Name).IsRequired().HasMaxLength(Board.NameMaxLength).UseCollation("NOCASE");
            board.HasIndex(b => b.Name).IsUnique();
            board.Property(b => b.Description).IsRequired().HasMaxLength(Board.DescriptionMaxLength);

            // SQLite stores decimals as TEXT, so they cannot be compared or sorted in SQL. We only filter by name.
            board.Property(b => b.LengthMm).HasPrecision(6, Board.DimensionDecimals);
            board.Property(b => b.WidthMm).HasPrecision(6, Board.DimensionDecimals);

            // Many-to-many without extra data: EF maintains the join table BoardComponents. Deleting a board
            // removes its links (Cascade); a component that is still linked cannot be deleted (Restrict).
            board.HasMany(b => b.Components)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "BoardComponents",
                    link => link.HasOne<Component>().WithMany().HasForeignKey("ComponentId").OnDelete(DeleteBehavior.Restrict),
                    link => link.HasOne<Board>().WithMany().HasForeignKey("BoardId").OnDelete(DeleteBehavior.Cascade));
        });
    }
}
