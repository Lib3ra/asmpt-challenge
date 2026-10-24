using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Api.Data;
using OrderManagement.Api.Tests.Infrastructure;

namespace OrderManagement.Api.Tests.Features.Boards;

/// <summary>
/// Checks the foreign key rules of the BoardComponents join table in SQLite itself, independent of the
/// checks the endpoints do first.
/// </summary>
[TestFixture]
public sealed class BoardDatabaseRulesTests
{
    private string _databasePath = null!;
    private SqliteApiFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void StartApplication()
    {
        _databasePath = SqliteApiFactory.CreateTemporaryDatabasePath();
        _factory = new SqliteApiFactory(_databasePath);
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void StopApplication()
    {
        _client.Dispose();
        _factory.Dispose();
        SqliteApiFactory.DeleteDatabase(_databasePath);
    }

    [Test]
    public async Task DeleteComponentInDatabase_WhenComponentIsOnBoard_ThrowsForeignKeyError()
    {
        // Arrange
        await _client.CreateBoardAsync("Controller board", 3);
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Act
        Func<Task> deleteComponent = () => db.Components.Where(c => c.Id == 3).ExecuteDeleteAsync();

        // Assert
        Assert.That(deleteComponent, Throws.InstanceOf<SqliteException>().With.Message.Contains("FOREIGN KEY"));
    }

    [Test]
    public async Task DeleteBoardInDatabase_WithComponents_RemovesItsComponentLinks()
    {
        // Arrange
        await _client.CreateBoardAsync("Controller board", 1, 3);
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Act
        await db.Boards.ExecuteDeleteAsync();
        int remainingLinks = await db.Set<Dictionary<string, object>>("BoardComponents").CountAsync();

        // Assert
        Assert.That(remainingLinks, Is.Zero);
    }
}
