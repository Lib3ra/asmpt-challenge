using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using OrderManagement.Api.Features.Health;

namespace OrderManagement.Api.Tests.Features.Health;

[TestFixture]
public sealed class HealthEndpointTests
{
    private WebApplicationFactory<Program> _factory = null!;

    [OneTimeSetUp]
    public void StartApplication() => _factory = new WebApplicationFactory<Program>();

    [OneTimeTearDown]
    public void StopApplication() => _factory.Dispose();

    [Test]
    public async Task GetHealth_WhenAppIsRunning_ReturnsHealthyAsJson()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(HealthEndpoints.Route);

        // Assert
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
            Assert.That(body?.Status, Is.EqualTo("Healthy"));
        }
    }
}
