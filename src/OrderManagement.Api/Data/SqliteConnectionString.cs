using Microsoft.Data.Sqlite;

namespace OrderManagement.Api.Data;

public static class SqliteConnectionString
{
    /// <summary>
    /// Makes a relative <c>Data Source</c> absolute against <paramref name="basePath"/>, so the database
    /// location does not depend on the process working directory. In-memory databases are left unchanged.
    /// </summary>
    public static string ResolveDataSource(string connectionString, string basePath)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (IsInMemory(builder) || Path.IsPathRooted(builder.DataSource))
        {
            return builder.ToString();
        }

        builder.DataSource = Path.GetFullPath(Path.Combine(basePath, builder.DataSource));
        return builder.ToString();
    }

    /// <summary>Creates the directory that will hold the database file. SQLite creates the file, not the directory.</summary>
    public static void EnsureDirectoryExists(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (IsInMemory(builder))
        {
            return;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(builder.DataSource));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static bool IsInMemory(SqliteConnectionStringBuilder builder) =>
        builder.Mode == SqliteOpenMode.Memory
        || string.IsNullOrEmpty(builder.DataSource)
        || builder.DataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase);
}
