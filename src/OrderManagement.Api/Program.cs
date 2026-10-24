using OrderManagement.Api.Data;
using OrderManagement.Api.Features.Components;
using OrderManagement.Api.Features.Health;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddDatabase(builder.Configuration, builder.Environment);
builder.Services.AddScoped<IComponentRepository, ComponentRepository>();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

// Error responses (404, 500, ...) use the RFC 9457 problem details format.
builder.Services.AddProblemDetails();

WebApplication app = builder.Build();

await app.ApplyMigrationsAsync();

// Logs unhandled exceptions and answers with a 500 problem details response.
app.UseExceptionHandler();
app.UseStatusCodePages();

// Serves the built React app from wwwroot. It only exists in the Docker image;
// in development Vite serves the frontend.
if (Directory.Exists(app.Environment.WebRootPath))
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.MapHealthEndpoints();
app.MapComponentEndpoints();

// Unknown API routes return 404 instead of falling through to the React app.
app.Map("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

await app.RunAsync();
