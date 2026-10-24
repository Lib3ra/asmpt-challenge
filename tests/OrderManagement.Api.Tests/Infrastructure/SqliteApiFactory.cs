using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using OrderManagement.Api.Data;

namespace OrderManagement.Api.Tests.Infrastructure;

/// <summary>
/// Runs the API in memory against its own SQLite file in a temporary directory,
/// so tests never touch the developer database.
/// </summary>
public sealed class SqliteApiFactory : WebApplicationFactory<Program>
{
    public SqliteApiFactory(string? databasePath = null)
    {
        DatabasePath = databasePath ?? CreateTemporaryDatabasePath();
    }

    public string DatabasePath { get; }

    public static string CreateTemporaryDatabasePath() =>
        Path.Combine(Path.GetTempPath(), "order-manager-tests", Guid.NewGuid().ToString("N"), "app.db");

    public static void DeleteDatabase(string databasePath)
    {
        // Pooled connections keep the file open; release them before deleting.
        SqliteConnection.ClearAllPools();
        string? directory = Path.GetDirectoryName(databasePath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting($"ConnectionStrings:{DatabaseExtensions.ConnectionStringName}", $"Data Source={DatabasePath}");
}
