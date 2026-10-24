using System.Net.Mime;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.Common.Endpoints;
using OrderManagement.Api.Common.Validation;
using OrderManagement.Api.Features.Boards;

namespace OrderManagement.Api.Features.Orders;

public static class OrderEndpoints
{
    public const string Route = "/api/orders";
    private const string EntityName = "order";
    private const string BoardIdsField = "boardIds";

    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(Route).WithTags("Orders");
        group.MapGet("/", SearchAsync)
            .WithSummary("List orders with their boards")
            .WithDescription("Newest order date first. The optional 'name' filter matches case-insensitively anywhere in the name.");
        group.MapGet("/{id:int}", GetByIdAsync)
            .WithSummary("Get one order with its boards");
        group.MapPost("/", CreateAsync)
            .WithSummary("Create one order")
            .WithDescription("Name: required, max 256, unique ignoring case. Description: optional, trimmed, max 2000. "
                + "orderDate: required, date only (yyyy-MM-dd). boardIds: at least one existing board, each at most once.");
        group.MapPut("/{id:int}", UpdateAsync)
            .WithSummary("Update one order")
            .WithDescription("Same rules as create. boardIds replaces the order's boards.");
        group.MapDelete("/", DeleteAsync)
            .WithSummary("Delete one or more orders")
            .WithDescription("Pass the ids as '?ids=1&ids=2'. All-or-nothing: if any id is unknown (404), nothing is deleted. "
                + "The links to boards are removed; the boards remain.");
        group.MapGet("/{id:int}/production-download", DownloadForProductionAsync)
            .WithSummary("Download an order for production")
            .WithDescription("Simulates handing an order to a production line: returns the order with its boards and their "
                + "components (including Quantity per board) as a JSON file named 'order-<id>-<name>.json', plus the UTC "
                + "time of the export. A pure export: the order is not changed and stays editable.");
        return app;
    }

    private static async Task<Ok<List<OrderResponse>>> SearchAsync(
        string? name, IOrderRepository orderRepository, CancellationToken cancellationToken)
    {
        IReadOnlyList<Order> orders = await orderRepository.SearchOrdersAsync(name, cancellationToken);
        return TypedResults.Ok(orders.Select(OrderResponse.From).ToList());
    }

    private static async Task<Results<Ok<OrderResponse>, NotFound>> GetByIdAsync(
        int id, IOrderRepository orderRepository, CancellationToken cancellationToken)
    {
        Order? order = await orderRepository.GetOrderByIdAsync(id, cancellationToken);
        return order is null ? TypedResults.NotFound() : TypedResults.Ok(OrderResponse.From(order));
    }

    private static async Task<Results<Created<OrderResponse>, ValidationProblem>> CreateAsync(
        OrderRequest request,
        IValidator<OrderRequest> orderValidator,
        IOrderRepository orderRepository,
        IBoardRepository boardRepository,
        CancellationToken cancellationToken)
    {
        // Step 1: rules that need no database. Invalid requests stop here without a database query.
        ValidationResult validation = await orderValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToProblemErrors());
        }

        // Step 2: unique name and existing boards.
        Dictionary<string, string[]> databaseErrors = await CheckAgainstDatabaseAsync(
            request, excludeId: null, orderRepository, boardRepository, cancellationToken);
        if (databaseErrors.Count > 0)
        {
            return TypedResults.ValidationProblem(databaseErrors);
        }

        Order order = await orderRepository.CreateOrderAsync(request.ToOrder(), request.BoardIdList, cancellationToken);
        return TypedResults.Created($"{Route}/{order.Id}", OrderResponse.From(order));
    }

    private static async Task<Results<Ok<OrderResponse>, NotFound, ValidationProblem>> UpdateAsync(
        int id,
        OrderRequest request,
        IValidator<OrderRequest> orderValidator,
        IOrderRepository orderRepository,
        IBoardRepository boardRepository,
        CancellationToken cancellationToken)
    {
        if (await orderRepository.GetOrderByIdAsync(id, cancellationToken) is null)
        {
            return TypedResults.NotFound();
        }

        // Step 1: rules that need no database. Invalid requests stop here without a database query.
        ValidationResult validation = await orderValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToProblemErrors());
        }

        // Step 2: unique name and existing boards.
        Dictionary<string, string[]> databaseErrors = await CheckAgainstDatabaseAsync(
            request, excludeId: id, orderRepository, boardRepository, cancellationToken);
        if (databaseErrors.Count > 0)
        {
            return TypedResults.ValidationProblem(databaseErrors);
        }

        Order? order = await orderRepository.UpdateOrderAsync(request.ToOrder(id), request.BoardIdList, cancellationToken);
        return order is null ? TypedResults.NotFound() : TypedResults.Ok(OrderResponse.From(order));
    }

    /// <summary>Deletes one or more orders (<c>?ids=1&amp;ids=2</c>). All-or-nothing: unknown ids (404) delete nothing.</summary>
    private static async Task<Results<NoContent, NotFound<ProblemDetails>, ValidationProblem>> DeleteAsync(
        int[]? ids, IOrderRepository orderRepository, CancellationToken cancellationToken)
    {
        if (ids is null || ids.Length == 0)
        {
            return ProblemResults.IdsRequired();
        }

        List<int> distinctIds = ids.Distinct().ToList();
        IReadOnlyList<int> missingIds = await orderRepository.FindMissingOrderIdsAsync(distinctIds, cancellationToken);
        if (missingIds.Count > 0)
        {
            return ProblemResults.IdsNotFound(EntityName, missingIds);
        }

        await orderRepository.DeleteOrdersAsync(distinctIds, cancellationToken);
        return TypedResults.NoContent();
    }

    /// <summary>
    /// Returns the order as a typed JSON result, so OpenAPI documents the file format; the Content-Disposition
    /// header makes browsers save it as a file.
    /// </summary>
    private static async Task<Results<Ok<ProductionFile>, NotFound>> DownloadForProductionAsync(
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

        ProductionFile productionFile = ProductionFile.From(order, DateTimeOffset.UtcNow);
        string fileName = ProductionFileName.For(order.Id, order.Name);
        httpContext.Response.Headers.ContentDisposition = new ContentDisposition { FileName = fileName }.ToString();

        loggerFactory.CreateLogger(typeof(OrderEndpoints)).LogInformation(
            "Order {OrderId} ({OrderName}) downloaded for production as {FileName} with {BoardCount} board(s)",
            order.Id, order.Name, fileName, order.Boards.Count);
        return TypedResults.Ok(productionFile);
    }

    private static async Task<Dictionary<string, string[]>> CheckAgainstDatabaseAsync(
        OrderRequest request,
        int? excludeId,
        IOrderRepository orderRepository,
        IBoardRepository boardRepository,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string[]> errors = [];
        if (await orderRepository.NameExistsAsync(request.NormalizedName, excludeId, cancellationToken))
        {
            errors["name"] = [NameConflicts.Message(EntityName, request.NormalizedName)];
        }

        IReadOnlyList<int> missingBoardIds = await boardRepository.FindMissingBoardIdsAsync(request.BoardIdList.ToList(), cancellationToken);
        if (missingBoardIds.Count > 0)
        {
            errors[BoardIdsField] = [$"No board exists with id {string.Join(", ", missingBoardIds)}."];
        }

        return errors;
    }
}
