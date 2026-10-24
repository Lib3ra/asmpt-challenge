using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.Features.Boards;
using OrderManagement.Api.Features.Orders;
using OrderManagement.Api.Tests.Infrastructure;

namespace OrderManagement.Api.Tests.Features.Orders;

/// <summary>Runs the order API against a fresh SQLite database per test (seeded with the five component presets).</summary>
[TestFixture]
public sealed class OrderEndpointTests
{
    private const string Route = OrderEndpoints.Route;

    private string _databasePath = null!;
    private SqliteApiFactory _factory = null!;
    private HttpClient _client = null!;
    private BoardResponse _controllerBoard = null!;
    private BoardResponse _powerBoard = null!;

    [SetUp]
    public async Task StartApplication()
    {
        _databasePath = SqliteApiFactory.CreateTemporaryDatabasePath();
        _factory = new SqliteApiFactory(_databasePath);
        _client = _factory.CreateClient();
        _controllerBoard = await _client.CreateBoardAsync("Controller board", 1, 3);
        _powerBoard = await _client.CreateBoardAsync("Power board", 5);
    }

    [TearDown]
    public void StopApplication()
    {
        _client.Dispose();
        _factory.Dispose();
        SqliteApiFactory.DeleteDatabase(_databasePath);
    }

    [Test]
    public async Task Create_WithValidRequest_StoresOrderWithBoards()
    {
        // Arrange
        OrderRequest request = new(" ORD-2026-007 ", "Prototype run", new DateOnly(2026, 10, 24), [_powerBoard.Id, _controllerBoard.Id]);

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(Route, request);
        OrderResponse? fetched = await _client.GetFromJsonAsync<OrderResponse>(response.Headers.Location);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That((fetched!.Name, fetched.Description, fetched.OrderDate),
                Is.EqualTo(("ORD-2026-007", "Prototype run", new DateOnly(2026, 10, 24))));
            Assert.That(fetched.Boards.Select(b => b.Name), Is.EqualTo(new[] { "Controller board", "Power board" }));
        }
    }

    [Test]
    public async Task Create_SendsOrderDateAsPlainDate()
    {
        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(Route, new OrderRequest("ORD-1", null, new DateOnly(2026, 1, 5), [_powerBoard.Id]));
        string json = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.That(json, Does.Contain("\"orderDate\":\"2026-01-05\""));
    }

    [Test]
    public async Task Create_WithUnknownBoardAndMissingDate_IsRejected()
    {
        // Arrange
        OrderRequest request = new("ORD-1", null, null, [_powerBoard.Id, 99]);

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(Route, request);
        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        // Assert: the date is checked first; unknown boards are only reported for otherwise valid requests.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(problem!.Errors.Keys, Is.EquivalentTo(new[] { "orderDate" }));
        }
    }

    [Test]
    public async Task Create_WithUnknownBoard_IsRejected()
    {
        // Arrange
        OrderRequest request = new("ORD-1", null, new DateOnly(2026, 10, 24), [_powerBoard.Id, 99]);

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(Route, request);
        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        // Assert
        Assert.That(problem!.Errors["boardIds"], Is.EqualTo(new[] { "No board exists with id 99." }));
    }

    [Test]
    public async Task Create_WithNameOfExistingOrderInDifferentCase_IsRejected()
    {
        // Arrange
        await _client.CreateOrderAsync("ORD-2026-001", _powerBoard.Id);

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            Route, new OrderRequest("ord-2026-001", null, new DateOnly(2026, 10, 24), [_controllerBoard.Id]));
        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        // Assert
        Assert.That(problem!.Errors["name"], Is.EqualTo(new[] { "An order named 'ord-2026-001' already exists." }));
    }

    [Test]
    public async Task Search_ReturnsNewestOrderDateFirstAndFiltersByName()
    {
        // Arrange
        await _client.PostAsJsonAsync(Route, new OrderRequest("Older run", null, new DateOnly(2026, 9, 1), [_powerBoard.Id]));
        await _client.PostAsJsonAsync(Route, new OrderRequest("Newer run", null, new DateOnly(2026, 10, 1), [_powerBoard.Id]));
        await _client.PostAsJsonAsync(Route, new OrderRequest("Prototype", null, new DateOnly(2026, 11, 1), [_powerBoard.Id]));

        // Act
        List<OrderResponse>? filtered = await _client.GetFromJsonAsync<List<OrderResponse>>($"{Route}?name=RUN");

        // Assert
        Assert.That(filtered!.Select(o => o.Name), Is.EqualTo(new[] { "Newer run", "Older run" }));
    }

    [Test]
    public async Task Update_WithNewBoards_ReplacesBoardLinks()
    {
        // Arrange
        OrderResponse order = await _client.CreateOrderAsync("ORD-1", _controllerBoard.Id, _powerBoard.Id);

        // Act
        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"{Route}/{order.Id}", new OrderRequest("ORD-1", "Rescheduled", new DateOnly(2026, 12, 1), [_powerBoard.Id]));
        OrderResponse? fetched = await _client.GetFromJsonAsync<OrderResponse>($"{Route}/{order.Id}");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That((fetched!.Description, fetched.OrderDate), Is.EqualTo(("Rescheduled", new DateOnly(2026, 12, 1))));
            Assert.That(fetched.Boards.Select(b => b.Id), Is.EqualTo(new[] { _powerBoard.Id }));
        }
    }

    [Test]
    public async Task Update_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        // Act
        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"{Route}/999", new OrderRequest("X", null, new DateOnly(2026, 10, 24), [_powerBoard.Id]));

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetById_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync($"{Route}/999");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Delete_WithSeveralIds_DeletesOrdersAndKeepsBoards()
    {
        // Arrange
        OrderResponse first = await _client.CreateOrderAsync("ORD-1", _controllerBoard.Id);
        OrderResponse second = await _client.CreateOrderAsync("ORD-2", _controllerBoard.Id, _powerBoard.Id);

        // Act
        HttpResponseMessage response = await _client.DeleteAsync($"{Route}?ids={first.Id}&ids={second.Id}");
        List<OrderResponse>? orders = await _client.GetFromJsonAsync<List<OrderResponse>>(Route);
        List<BoardResponse>? boards = await _client.GetFromJsonAsync<List<BoardResponse>>(BoardEndpoints.Route);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(orders, Is.Empty);
            Assert.That(boards, Has.Count.EqualTo(2));
        }
    }

    [Test]
    public async Task Delete_WhenOneIdDoesNotExist_DeletesNothing()
    {
        // Arrange
        OrderResponse order = await _client.CreateOrderAsync("ORD-1", _powerBoard.Id);

        // Act
        HttpResponseMessage response = await _client.DeleteAsync($"{Route}?ids={order.Id}&ids=999");
        List<OrderResponse>? orders = await _client.GetFromJsonAsync<List<OrderResponse>>(Route);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(orders, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task DeleteBoard_WhenBoardIsPartOfOrder_ReturnsConflictAndDeletesNothing()
    {
        // Arrange
        await _client.CreateOrderAsync("ORD-1", _controllerBoard.Id);

        // Act
        HttpResponseMessage response = await _client.DeleteAsync($"{BoardEndpoints.Route}?ids={_controllerBoard.Id}&ids={_powerBoard.Id}");
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        List<BoardResponse>? boards = await _client.GetFromJsonAsync<List<BoardResponse>>(BoardEndpoints.Route);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(problem!.Detail, Is.EqualTo("Still part of orders: 'Controller board' in ORD-1. Nothing was deleted."));
            Assert.That(boards, Has.Count.EqualTo(2));
        }
    }
}
