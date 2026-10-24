using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace OrderManagement.Api.Data;

public static class DatabaseExtensions
{
    public const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            var configured = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");
            options.UseSqlite(SqliteConnectionString.ResolveDataSource(configured, environment.ContentRootPath));
        });
        return services;
    }

    /// <summary>Applies pending EF Core migrations. Runs once at startup; the app is operated as a single instance.</summary>
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        var connectionString = db.Database.GetConnectionString()!;
        SqliteConnectionString.EnsureDirectoryExists(connectionString);

        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count == 0)
        {
            logger.LogInformation("Database {DataSource} is up to date", dataSource);
            return;
        }

        logger.LogInformation("Applying {MigrationCount} migration(s) to {DataSource}: {Migrations}",
            pending.Count, dataSource, string.Join(", ", pending));
        await db.Database.MigrateAsync();
    }
}
