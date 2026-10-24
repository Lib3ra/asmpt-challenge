using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Api.Data;
using OrderManagement.Api.Features.Boards;
using OrderManagement.Api.Tests.Infrastructure;

namespace OrderManagement.Api.Tests.Features.Orders;

/// <summary>Checks the foreign key rules of the OrderBoards join table in SQLite itself.</summary>
[TestFixture]
public sealed class OrderDatabaseRulesTests
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
    public async Task DeletingBoardInOrderDirectly_IsRejectedByForeignKey()
    {
        // Arrange
        BoardResponse board = await _client.CreateBoardAsync("Controller board", 1);
        await _client.CreateOrderAsync("ORD-1", board.Id);
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Act
        Func<Task> deleteBoard = () => db.Boards.Where(b => b.Id == board.Id).ExecuteDeleteAsync();

        // Assert
        Assert.That(deleteBoard, Throws.InstanceOf<SqliteException>().With.Message.Contains("FOREIGN KEY"));
    }

    [Test]
    public async Task DeletingOrder_RemovesItsBoardLinks()
    {
        // Arrange
        BoardResponse board = await _client.CreateBoardAsync("Controller board", 1);
        await _client.CreateOrderAsync("ORD-1", board.Id);
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Act
        await db.Orders.ExecuteDeleteAsync();
        int remainingLinks = await db.Set<Dictionary<string, object>>("OrderBoards").CountAsync();

        // Assert
        Assert.That(remainingLinks, Is.Zero);
    }
}
