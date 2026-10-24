using System.Net;
using System.Net.Http.Json;
using OrderManagement.Api.Data;
using OrderManagement.Api.Features.Components;
using OrderManagement.Api.Tests.Infrastructure;

namespace OrderManagement.Api.Tests.Features.Components;

[TestFixture]
public sealed class ComponentPersistenceTests
{
    private string _databasePath = null!;

    [SetUp]
    public void CreateDatabasePath() => _databasePath = SqliteApiFactory.CreateTemporaryDatabasePath();

    [TearDown]
    public void DeleteDatabase() => SqliteApiFactory.DeleteDatabase(_databasePath);

    [Test]
    public async Task GetComponents_OnFreshDatabase_ReturnsSeededPresets()
    {
        // Arrange
        using SqliteApiFactory factory = new(_databasePath);
        using HttpClient client = factory.CreateClient();

        // Act
        List<ComponentResponse>? components = await client.GetFromJsonAsync<List<ComponentResponse>>(ComponentEndpoints.Route);

        // Assert
        Assert.That(components, Is.EqualTo(SeedData.Components.Select(ComponentResponse.From)));
    }

    [Test]
    public async Task CreateComponent_WithQuantity_IsReturnedByGetById()
    {
        // Arrange
        using SqliteApiFactory factory = new(_databasePath);
        using HttpClient client = factory.CreateClient();
        CreateComponentRequest request = new("Resistor 4.7 kOhm", "0603, 1%", 12);

        // Act
        HttpResponseMessage createResponse = await client.PostAsJsonAsync(ComponentEndpoints.Route, request);
        ComponentResponse? created = await createResponse.Content.ReadFromJsonAsync<ComponentResponse>();
        ComponentResponse? fetched = await client.GetFromJsonAsync<ComponentResponse>($"{ComponentEndpoints.Route}/{created!.Id}");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(createResponse.Headers.Location?.ToString(), Is.EqualTo($"{ComponentEndpoints.Route}/{created.Id}"));
            Assert.That(fetched, Is.EqualTo(new ComponentResponse(created.Id, "Resistor 4.7 kOhm", "0603, 1%", 12)));
        }
    }

    [Test]
    public async Task CreateComponent_ThenRestartApplication_ComponentIsStillStored()
    {
        // Arrange
        ComponentResponse? created;
        using (SqliteApiFactory firstRun = new(_databasePath))
        using (HttpClient client = firstRun.CreateClient())
        {
            HttpResponseMessage response = await client.PostAsJsonAsync(
                ComponentEndpoints.Route, new CreateComponentRequest("Capacitor 10 uF", null, 4));
            created = await response.Content.ReadFromJsonAsync<ComponentResponse>();
        }

        // Act
        using SqliteApiFactory secondRun = new(_databasePath);
        using HttpClient restartedClient = secondRun.CreateClient();
        List<ComponentResponse>? components = await restartedClient.GetFromJsonAsync<List<ComponentResponse>>(ComponentEndpoints.Route);

        // Assert
        Assert.That(components, Does.Contain(created).And.Count.EqualTo(SeedData.Components.Length + 1));
    }

    [Test]
    public async Task GetComponentById_WhenMissing_ReturnsNotFound()
    {
        // Arrange
        using SqliteApiFactory factory = new(_databasePath);
        using HttpClient client = factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync($"{ComponentEndpoints.Route}/999");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
