using FluentValidation;
using OrderManagement.Api.Data;
using OrderManagement.Api.Features.Boards;
using OrderManagement.Api.Features.Components;
using OrderManagement.Api.Features.Health;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddDatabase(builder.Configuration, builder.Environment);
builder.Services.AddScoped<IComponentRepository, ComponentRepository>();
builder.Services.AddScoped<IBoardRepository, BoardRepository>();

// Registers every FluentValidation validator in this assembly. Validators are stateless, so one instance is shared.
builder.Services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Singleton);
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

// Error responses (404, 500, ...) use the RFC 9457 problem details format.
builder.Services.AddProblemDetails();

// OpenAPI document at /openapi/v1.json, generated from the endpoint definitions.
builder.Services.AddOpenApi();

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

// API documentation is available in every environment: the app has no authentication and the
// containerized demo runs in Production. Swagger UI at /swagger reads the generated document.
app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Order Manager API");
    options.DocumentTitle = "Order Manager API";
});

app.MapHealthEndpoints();
app.MapComponentEndpoints();
app.MapBoardEndpoints();

// Unknown API routes return 404 instead of falling through to the React app.
app.Map("/api/{**path}", () => Results.NotFound()).ExcludeFromDescription();
app.MapFallbackToFile("index.html");

await app.RunAsync();
