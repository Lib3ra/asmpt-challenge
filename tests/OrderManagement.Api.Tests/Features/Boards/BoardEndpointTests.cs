using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.Data;
using OrderManagement.Api.Features.Boards;
using OrderManagement.Api.Features.Components;
using OrderManagement.Api.Tests.Infrastructure;

namespace OrderManagement.Api.Tests.Features.Boards;

/// <summary>Runs the board API against a fresh SQLite database per test (seeded with the five component presets).</summary>
[TestFixture]
public sealed class BoardEndpointTests
{
    private const string Route = BoardEndpoints.Route;

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
    public async Task Create_WithValidRequest_StoresBoardWithComponents()
    {
        // Arrange
        BoardRequest request = new(" Controller board ", " Rev B ", 100.5m, 80.25m, [3, 1]);

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(Route, request);
        BoardResponse? created = await response.Content.ReadFromJsonAsync<BoardResponse>();
        BoardResponse? fetched = await _client.GetFromJsonAsync<BoardResponse>(response.Headers.Location);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That((created!.Name, created.Description, created.LengthMm, created.WidthMm),
                Is.EqualTo(("Controller board", "Rev B", 100.5m, 80.25m)));
            Assert.That(fetched!.Components.Select(c => c.Name), Is.EqualTo(new[] { "LED green", "Resistor 10 kOhm" }));
            Assert.That(fetched.Components[1].Quantity, Is.EqualTo(24));
        }
    }

    [Test]
    public async Task Create_WithUnknownComponent_IsRejected()
    {
        // Arrange
        BoardRequest request = new("Controller board", null, 100m, 80m, [1, 99]);

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(Route, request);
        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(problem!.Errors["componentIds"], Is.EqualTo(new[] { "No component exists with id 99." }));
        }
    }

    [Test]
    public async Task Create_WithoutComponents_IsRejected()
    {
        // Arrange
        BoardRequest request = new("Controller board", null, 100m, 80m, []);

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(Route, request);
        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        // Assert
        Assert.That(problem!.Errors["componentIds"], Is.EqualTo(new[] { "A board needs at least one component." }));
    }

    [Test]
    public async Task Create_WithNameOfExistingBoardInDifferentCase_IsRejected()
    {
        // Arrange
        await _client.CreateBoardAsync("Controller board", 1);

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(Route, new BoardRequest("CONTROLLER board", null, 50m, 50m, [2]));
        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        // Assert
        Assert.That(problem!.Errors["name"], Is.EqualTo(new[] { "A board named 'CONTROLLER board' already exists." }));
    }

    [Test]
    public async Task Search_WithPartOfNameInDifferentCase_ReturnsMatchingBoards()
    {
        // Arrange
        await _client.CreateBoardAsync("Controller board", 1);
        await _client.CreateBoardAsync("Power board", 5);

        // Act
        List<BoardResponse>? boards = await _client.GetFromJsonAsync<List<BoardResponse>>($"{Route}?name=POWER");

        // Assert
        Assert.That(boards!.Select(b => b.Name), Is.EqualTo(new[] { "Power board" }));
    }

    [Test]
    public async Task Update_WithNewComponents_ReplacesComponentLinks()
    {
        // Arrange
        BoardResponse board = await _client.CreateBoardAsync("Controller board", 1, 2);

        // Act
        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"{Route}/{board.Id}", new BoardRequest("Controller board", "Rev C", 120m, 90m, [4]));
        BoardResponse? fetched = await _client.GetFromJsonAsync<BoardResponse>($"{Route}/{board.Id}");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That((fetched!.Description, fetched.LengthMm), Is.EqualTo(("Rev C", 120m)));
            Assert.That(fetched.Components.Select(c => c.Id), Is.EqualTo(new[] { 4 }));
        }
    }

    [Test]
    public async Task Update_WhenBoardDoesNotExist_ReturnsNotFound()
    {
        // Act
        HttpResponseMessage response = await _client.PutAsJsonAsync($"{Route}/999", new BoardRequest("X", null, 1m, 1m, [1]));

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetById_WhenBoardDoesNotExist_ReturnsNotFound()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync($"{Route}/999");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Delete_WithSeveralIds_DeletesBoardsAndKeepsComponents()
    {
        // Arrange
        BoardResponse first = await _client.CreateBoardAsync("Controller board", 1);
        BoardResponse second = await _client.CreateBoardAsync("Power board", 1, 5);

        // Act
        HttpResponseMessage response = await _client.DeleteAsync($"{Route}?ids={first.Id}&ids={second.Id}");
        List<BoardResponse>? boards = await _client.GetFromJsonAsync<List<BoardResponse>>(Route);
        List<ComponentResponse>? components = await _client.GetFromJsonAsync<List<ComponentResponse>>(ComponentEndpoints.Route);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(boards, Is.Empty);
            Assert.That(components, Has.Count.EqualTo(SeedData.Components.Length));
        }
    }

    [Test]
    public async Task Delete_WhenOneIdDoesNotExist_DeletesNothing()
    {
        // Arrange
        BoardResponse board = await _client.CreateBoardAsync("Controller board", 1);

        // Act
        HttpResponseMessage response = await _client.DeleteAsync($"{Route}?ids={board.Id}&ids=999");
        List<BoardResponse>? boards = await _client.GetFromJsonAsync<List<BoardResponse>>(Route);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(boards, Has.Count.EqualTo(1));
        }
    }
}
