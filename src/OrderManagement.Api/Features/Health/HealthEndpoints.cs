using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OrderManagement.Api.Features.Health;

public static class HealthEndpoints
{
    public const string Route = "/api/health";

    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks(Route, new HealthCheckOptions { ResponseWriter = WriteJsonAsync });
        return app;
    }

    private static Task WriteJsonAsync(HttpContext context, HealthReport report) =>
        context.Response.WriteAsJsonAsync(new HealthResponse(report.Status.ToString()));
}

public sealed record HealthResponse(string Status);
