using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.Features.Boards;
using OrderManagement.Api.Features.Search;
using OrderManagement.Api.Tests.Infrastructure;

namespace OrderManagement.Api.Tests.Features.Search;

/// <summary>
/// Runs the global search against a fresh SQLite database per test. The seeded components include "LED green";
/// each test adds a board and an order whose names also contain "led".
/// </summary>
[TestFixture]
public sealed class SearchEndpointTests
{
    private const string Route = SearchEndpoints.Route;

    private string _databasePath = null!;
    private SqliteApiFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public async Task StartApplication()
    {
        _databasePath = SqliteApiFactory.CreateTemporaryDatabasePath();
        _factory = new SqliteApiFactory(_databasePath);
        _client = _factory.CreateClient();
        BoardResponse ledBoard = await _client.CreateBoardAsync("LED driver board", 3);
        await _client.CreateBoardAsync("Power board", 5);
        await _client.CreateOrderAsync("Sample run for LED panel", ledBoard.Id);
    }

    [TearDown]
    public void StopApplication()
    {
        _client.Dispose();
        _factory.Dispose();
        SqliteApiFactory.DeleteDatabase(_databasePath);
    }

    [Test]
    public async Task Search_WithTermInDifferentCase_ReturnsMatchesGroupedByType()
    {
        // Act
        SearchResponse? result = await _client.GetFromJsonAsync<SearchResponse>($"{Route}?term=Led");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result!.Orders.Select(hit => hit.Name), Is.EqualTo(new[] { "Sample run for LED panel" }));
            Assert.That(result.Boards.Select(hit => hit.Name), Is.EqualTo(new[] { "LED driver board" }));
            Assert.That(result.Components.Select(hit => hit.Name), Is.EqualTo(new[] { "LED green" }));
        }
    }

    [Test]
    public async Task Search_WithType_ReturnsOnlyThatType()
    {
        // Act
        SearchResponse? result = await _client.GetFromJsonAsync<SearchResponse>($"{Route}?term=led&type=boards");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result!.Boards.Select(hit => hit.Name), Is.EqualTo(new[] { "LED driver board" }));
            Assert.That(result.Orders, Is.Empty);
            Assert.That(result.Components, Is.Empty);
        }
    }

    [Test]
    public async Task Search_WithoutMatches_ReturnsEmptyGroups()
    {
        // Act
        SearchResponse? result = await _client.GetFromJsonAsync<SearchResponse>($"{Route}?term=transformer");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result!.Orders, Is.Empty);
            Assert.That(result.Boards, Is.Empty);
            Assert.That(result.Components, Is.Empty);
        }
    }

    [Test]
    public async Task Search_WithBlankTerm_ReturnsValidationProblem()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync($"{Route}?term=%20");
        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(problem!.Errors["term"], Is.EqualTo(new[] { "Search term is required." }));
        }
    }

    [Test]
    public async Task Search_WithUnknownType_ReturnsValidationProblem()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync($"{Route}?term=led&type=machines");
        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(problem!.Errors.Keys, Is.EquivalentTo(new[] { "type" }));
        }
    }
}
