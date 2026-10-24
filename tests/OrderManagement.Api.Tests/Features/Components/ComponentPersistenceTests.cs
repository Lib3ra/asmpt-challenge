using System.Net;
using System.Net.Http.Json;
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
    public async Task CreateComponent_WithQuantity_IsReturnedByGetById()
    {
        // Arrange
        using var factory = new SqliteApiFactory(_databasePath);
        using var client = factory.CreateClient();
        var request = new CreateComponentRequest("Resistor 10k", "0603, 1%", 12);

        // Act
        var createResponse = await client.PostAsJsonAsync(ComponentEndpoints.Route, request);
        var created = await createResponse.Content.ReadFromJsonAsync<ComponentResponse>();
        var fetched = await client.GetFromJsonAsync<ComponentResponse>($"{ComponentEndpoints.Route}/{created!.Id}");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(createResponse.Headers.Location?.ToString(), Is.EqualTo($"{ComponentEndpoints.Route}/{created.Id}"));
            Assert.That(fetched, Is.EqualTo(new ComponentResponse(created.Id, "Resistor 10k", "0603, 1%", 12)));
        }
    }

    [Test]
    public async Task CreateComponent_ThenRestartApplication_ComponentIsStillStored()
    {
        // Arrange
        using (var firstRun = new SqliteApiFactory(_databasePath))
        using (var client = firstRun.CreateClient())
        {
            await client.PostAsJsonAsync(ComponentEndpoints.Route, new CreateComponentRequest("Capacitor 100nF", null, 4));
        }

        // Act
        using var secondRun = new SqliteApiFactory(_databasePath);
        using var restartedClient = secondRun.CreateClient();
        var components = await restartedClient.GetFromJsonAsync<List<ComponentResponse>>(ComponentEndpoints.Route);

        // Assert
        Assert.That(components, Is.EqualTo(new[] { new ComponentResponse(1, "Capacitor 100nF", "", 4) }));
    }

    [Test]
    public async Task GetComponentById_WhenMissing_ReturnsNotFound()
    {
        // Arrange
        using var factory = new SqliteApiFactory(_databasePath);
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync($"{ComponentEndpoints.Route}/999");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
