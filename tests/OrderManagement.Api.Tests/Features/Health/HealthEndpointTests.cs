using System.Net;
using System.Net.Http.Json;
using OrderManagement.Api.Features.Health;
using OrderManagement.Api.Tests.Infrastructure;

namespace OrderManagement.Api.Tests.Features.Health;

[TestFixture]
public sealed class HealthEndpointTests
{
    private SqliteApiFactory _factory = null!;

    [OneTimeSetUp]
    public void StartApplication() => _factory = new SqliteApiFactory();

    [OneTimeTearDown]
    public void StopApplication()
    {
        _factory.Dispose();
        SqliteApiFactory.DeleteDatabase(_factory.DatabasePath);
    }

    [Test]
    public async Task GetHealth_WhenDatabaseIsReachable_ReturnsHealthyAsJson()
    {
        // Arrange
        using HttpClient client = _factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync(HealthEndpoints.Route);

        // Assert
        HealthResponse? body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
            Assert.That(body?.Status, Is.EqualTo("Healthy"));
        }
    }
}
