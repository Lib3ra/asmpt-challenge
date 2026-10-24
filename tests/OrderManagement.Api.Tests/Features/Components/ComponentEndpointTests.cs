using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.Data;
using OrderManagement.Api.Features.Components;
using OrderManagement.Api.Tests.Infrastructure;

namespace OrderManagement.Api.Tests.Features.Components;

/// <summary>Runs the component API against a fresh SQLite database per test (seeded with the five presets).</summary>
[TestFixture]
public sealed class ComponentEndpointTests
{
    private const string Route = ComponentEndpoints.Route;

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
    public async Task Search_WithoutName_ReturnsSeededPresetsOrderedByName()
    {
        // Act
        List<ComponentResponse>? components = await _client.GetFromJsonAsync<List<ComponentResponse>>(Route);

        // Assert
        Assert.That(components, Is.EqualTo(SeedData.Components.OrderBy(c => c.Name).Select(ComponentResponse.From)));
    }

    [Test]
    public async Task Search_WithPartOfNameInDifferentCase_ReturnsMatchingComponents()
    {
        // Act
        List<ComponentResponse>? components = await _client.GetFromJsonAsync<List<ComponentResponse>>($"{Route}?name=CAP");

        // Assert
        Assert.That(components!.Select(c => c.Name), Is.EqualTo(new[] { "Ceramic capacitor 100 nF" }));
    }

    [Test]
    public async Task Create_WithSeveralValidComponents_StoresAllAndReturnsCreated()
    {
        // Arrange
        ComponentRequest[] requests = [new("  Crystal 8 MHz ", "HC-49 SMD", 1), new("Diode 1N4148", null, 6)];

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(Route, requests);
        List<ComponentResponse>? created = await response.Content.ReadFromJsonAsync<List<ComponentResponse>>();
        ComponentResponse? fetched = await _client.GetFromJsonAsync<ComponentResponse>($"{Route}/{created![0].Id}");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(created.Select(c => (c.Name, c.Description, c.Quantity)),
                Is.EqualTo(new[] { ("Crystal 8 MHz", "HC-49 SMD", 1), ("Diode 1N4148", "", 6) }));
            Assert.That(fetched, Is.EqualTo(created[0]));
        }
    }

    [Test]
    public async Task Create_WhenOneItemIsInvalid_StoresNothingAndReportsItemErrors()
    {
        // Arrange
        ComponentRequest[] requests = [new("Crystal 8 MHz", null, 1), new("Diode 1N4148", null, 0)];

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(Route, requests);
        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        List<ComponentResponse>? stored = await _client.GetFromJsonAsync<List<ComponentResponse>>($"{Route}?name=Crystal");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(problem!.Errors.Keys, Is.EquivalentTo(new[] { "[1].quantity" }));
            Assert.That(stored, Is.Empty);
        }
    }

    [Test]
    public async Task Create_WithNameOfExistingComponentInDifferentCase_IsRejected()
    {
        // Arrange
        ComponentRequest[] requests = [new("led GREEN", null, 2)];

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(Route, requests);
        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(problem!.Errors["[0].name"], Is.EqualTo(new[] { "A component named 'led GREEN' already exists." }));
        }
    }

    [Test]
    public async Task Update_WithValidRequest_StoresChanges()
    {
        // Arrange
        ComponentRequest request = new("LED red", "0805 SMD LED, 2.0 V", 6);

        // Act
        HttpResponseMessage response = await _client.PutAsJsonAsync($"{Route}/3", request);
        ComponentResponse? fetched = await _client.GetFromJsonAsync<ComponentResponse>($"{Route}/3");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(fetched, Is.EqualTo(new ComponentResponse(3, "LED red", "0805 SMD LED, 2.0 V", 6)));
        }
    }

    [Test]
    public async Task Update_KeepingOwnNameInDifferentCase_IsAllowed()
    {
        // Arrange
        ComponentRequest request = new("LED GREEN", null, 4);

        // Act
        HttpResponseMessage response = await _client.PutAsJsonAsync($"{Route}/3", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Update_WithNameOfAnotherComponent_IsRejected()
    {
        // Arrange
        ComponentRequest request = new("Resistor 10 kOhm", null, 4);

        // Act
        HttpResponseMessage response = await _client.PutAsJsonAsync($"{Route}/3", request);
        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(problem!.Errors.Keys, Is.EquivalentTo(new[] { "name" }));
        }
    }

    [Test]
    public async Task Update_WhenComponentDoesNotExist_ReturnsNotFound()
    {
        // Act
        HttpResponseMessage response = await _client.PutAsJsonAsync($"{Route}/999", new ComponentRequest("X", null, 1));

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Delete_WithSeveralIds_DeletesAll()
    {
        // Act
        HttpResponseMessage response = await _client.DeleteAsync($"{Route}?ids=1&ids=2");
        List<ComponentResponse>? remaining = await _client.GetFromJsonAsync<List<ComponentResponse>>(Route);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(remaining!.Select(c => c.Id), Is.EquivalentTo(new[] { 3, 4, 5 }));
        }
    }

    [Test]
    public async Task Delete_WhenOneIdDoesNotExist_DeletesNothing()
    {
        // Act
        HttpResponseMessage response = await _client.DeleteAsync($"{Route}?ids=1&ids=999");
        List<ComponentResponse>? remaining = await _client.GetFromJsonAsync<List<ComponentResponse>>(Route);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(remaining, Has.Count.EqualTo(SeedData.Components.Length));
        }
    }

    [Test]
    public async Task Delete_WithoutIds_ReturnsBadRequest()
    {
        // Act
        HttpResponseMessage response = await _client.DeleteAsync(Route);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Create_ThenRestartApplication_ComponentIsStillStored()
    {
        // Arrange
        HttpResponseMessage response = await _client.PostAsJsonAsync(Route, new[] { new ComponentRequest("Capacitor 10 uF", null, 4) });
        List<ComponentResponse>? created = await response.Content.ReadFromJsonAsync<List<ComponentResponse>>();
        _client.Dispose();
        _factory.Dispose();
        _factory = new SqliteApiFactory(_databasePath);
        _client = _factory.CreateClient();

        // Act
        ComponentResponse? fetched = await _client.GetFromJsonAsync<ComponentResponse>($"{Route}/{created![0].Id}");

        // Assert
        Assert.That(fetched, Is.EqualTo(created[0]));
    }
}
