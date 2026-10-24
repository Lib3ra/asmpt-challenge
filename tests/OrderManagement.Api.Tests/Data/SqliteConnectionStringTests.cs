using OrderManagement.Api.Data;

namespace OrderManagement.Api.Tests.Data;

[TestFixture]
public sealed class SqliteConnectionStringTests
{
    private static readonly string BasePath = Path.Combine(Path.GetTempPath(), "content-root");

    [Test]
    public void ResolveDataSource_WithRelativePath_MakesItAbsoluteAgainstBasePath()
    {
        // Act
        var resolved = SqliteConnectionString.ResolveDataSource("Data Source=App_Data/app.db", BasePath);

        // Assert
        Assert.That(resolved, Is.EqualTo($"Data Source={Path.Combine(BasePath, "App_Data", "app.db")}"));
    }

    [Test]
    public void ResolveDataSource_WithAbsolutePath_KeepsPath()
    {
        // Arrange
        var absolute = Path.Combine(Path.GetTempPath(), "data", "app.db");

        // Act
        var resolved = SqliteConnectionString.ResolveDataSource($"Data Source={absolute}", BasePath);

        // Assert
        Assert.That(resolved, Is.EqualTo($"Data Source={absolute}"));
    }

    [TestCase("Data Source=:memory:")]
    [TestCase("Data Source=shared;Mode=Memory")]
    public void ResolveDataSource_WithInMemoryDatabase_KeepsDataSource(string connectionString)
    {
        // Act
        var resolved = SqliteConnectionString.ResolveDataSource(connectionString, BasePath);

        // Assert
        Assert.That(resolved, Does.Not.Contain(BasePath));
    }

    [Test]
    public void EnsureDirectoryExists_WhenDirectoryIsMissing_CreatesIt()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), "order-manager-tests", Guid.NewGuid().ToString("N"));
        var directory = Path.Combine(root, "nested", "data");

        try
        {
            // Act
            SqliteConnectionString.EnsureDirectoryExists($"Data Source={Path.Combine(directory, "app.db")}");

            // Assert
            Assert.That(Directory.Exists(directory), Is.True);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
