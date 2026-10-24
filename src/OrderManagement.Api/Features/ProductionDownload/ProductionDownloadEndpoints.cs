using System.Net.Mime;
using Microsoft.AspNetCore.Http.HttpResults;
using OrderManagement.Api.Features.Orders;

namespace OrderManagement.Api.Features.ProductionDownload;

public static class ProductionDownloadEndpoints
{
    public const string Route = "/api/orders/{id:int}/production-download";
    private const string LoggerCategory = "OrderManagement.Api.Features.ProductionDownload";

    public static IEndpointRouteBuilder MapProductionDownloadEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Route, DownloadAsync)
            .WithTags("Production download")
            .WithSummary("Download an order for production")
            .WithDescription("Simulates handing an order to a production line: returns the order with its boards and their "
                + "components (including Quantity per board) as a JSON file named 'order-<id>-<name>.json', plus the UTC "
                + "time of the export. A pure export: the order is not changed and stays editable.");
        return app;
    }

    private static async Task<Results<Ok<ProductionFile>, NotFound>> DownloadAsync(
        int id,
        IOrderRepository orderRepository,
        ILoggerFactory loggerFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        Order? order = await orderRepository.GetOrderForProductionAsync(id, cancellationToken);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        ProductionFile download = ProductionFile.From(order, DateTimeOffset.UtcNow);
        string fileName = ProductionFileName.For(order.Id, order.Name);

        // The body stays a typed JSON result (documented in OpenAPI); the header makes browsers save it as a file.
        httpContext.Response.Headers.ContentDisposition = new ContentDisposition { FileName = fileName }.ToString();

        loggerFactory.CreateLogger(LoggerCategory).LogInformation(
            "Order {OrderId} ({OrderName}) downloaded for production as {FileName} with {BoardCount} board(s)",
            order.Id, order.Name, fileName, order.Boards.Count);
        return TypedResults.Ok(download);
    }
}
