using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using OrderManagement.Api.Features.Health;

namespace OrderManagement.Api.Tests.Features.Health;

public sealed class HealthEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetHealth_WhenAppIsRunning_ReturnsHealthyAsJson()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(HealthEndpoints.Route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.Equal("Healthy", body?.Status);
    }
}
