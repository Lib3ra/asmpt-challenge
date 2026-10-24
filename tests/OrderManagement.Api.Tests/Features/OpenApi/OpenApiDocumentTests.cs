using System.Net;
using System.Text.Json;
using OrderManagement.Api.Features.Components;
using OrderManagement.Api.Tests.Infrastructure;

namespace OrderManagement.Api.Tests.Features.OpenApi;

[TestFixture]
public sealed class OpenApiDocumentTests
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
    public async Task GetOpenApiDocument_DescribesComponentEndpoints()
    {
        // Arrange
        using HttpClient client = _factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync("/openapi/v1.json");
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement paths = document.RootElement.GetProperty("paths");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(paths.TryGetProperty(ComponentEndpoints.Route, out _), Is.True);
            Assert.That(paths.TryGetProperty($"{ComponentEndpoints.Route}/{{id}}", out _), Is.True);
        }
    }

    [Test]
    public async Task GetSwaggerUi_ReturnsPage()
    {
        // Arrange
        using HttpClient client = _factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync("/swagger/index.html");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
