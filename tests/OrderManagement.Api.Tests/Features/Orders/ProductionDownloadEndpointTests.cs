using System.Net;
using System.Net.Http.Json;
using OrderManagement.Api.Features.Boards;
using OrderManagement.Api.Features.Orders;
using OrderManagement.Api.Tests.Infrastructure;

namespace OrderManagement.Api.Tests.Features.Orders;

/// <summary>Downloads orders against a fresh SQLite database per test (seeded with the five component presets).</summary>
[TestFixture]
public sealed class ProductionDownloadEndpointTests
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

    private static string RouteFor(int orderId) => $"/api/orders/{orderId}/production-download";

    [Test]
    public async Task Download_WhenOrderExists_ReturnsOrderWithBoardsAndComponentsAsJsonFile()
    {
        // Arrange
        BoardResponse powerBoard = await _client.CreateBoardAsync("Power board", 5);
        BoardResponse controllerBoard = await _client.CreateBoardAsync("Controller board", 3, 1);
        OrderResponse order = await _client.CreateOrderAsync("ORD-2026-001 Prototype run", powerBoard.Id, controllerBoard.Id);
        DateTimeOffset before = DateTimeOffset.UtcNow;

        // Act
        HttpResponseMessage response = await _client.GetAsync(RouteFor(order.Id));
        ProductionFile? file = await response.Content.ReadFromJsonAsync<ProductionFile>();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
            Assert.That(response.Content.Headers.ContentDisposition?.DispositionType, Is.EqualTo("attachment"));
            Assert.That(response.Content.Headers.ContentDisposition?.FileName, Is.EqualTo($"order-{order.Id}-ord-2026-001-prototype-run.json"));
            Assert.That(file!.ExportedAt, Is.InRange(before, DateTimeOffset.UtcNow));
            Assert.That(file.Order.Name, Is.EqualTo("ORD-2026-001 Prototype run"));
            Assert.That(file.Order.OrderDate, Is.EqualTo(new DateOnly(2026, 10, 24)));
            Assert.That(file.Order.Boards.Select(b => b.Name), Is.EqualTo(new[] { "Controller board", "Power board" }));
            Assert.That(file.Order.Boards[0].Components.Select(c => (c.Name, c.Quantity)),
                Is.EqualTo(new[] { ("LED green", 4), ("Resistor 10 kOhm", 24) }));
            Assert.That(file.Order.Boards[1].LengthMm, Is.EqualTo(100m));
        }
    }

    [Test]
    public async Task Download_WhenOrderExists_LeavesOrderUnchanged()
    {
        // Arrange
        BoardResponse board = await _client.CreateBoardAsync("Power board", 5);
        OrderResponse order = await _client.CreateOrderAsync("ORD-1", board.Id);

        // Act
        await _client.GetAsync(RouteFor(order.Id));
        OrderResponse? fetched = await _client.GetFromJsonAsync<OrderResponse>($"{OrderEndpoints.Route}/{order.Id}");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That((fetched!.Name, fetched.Description, fetched.OrderDate), Is.EqualTo((order.Name, order.Description, order.OrderDate)));
            Assert.That(fetched.Boards, Is.EqualTo(order.Boards));
        }
    }

    [Test]
    public async Task Download_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync(RouteFor(999));

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
