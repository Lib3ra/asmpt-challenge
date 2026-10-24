using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Features.Components;

namespace OrderManagement.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Component> Components => Set<Component>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
