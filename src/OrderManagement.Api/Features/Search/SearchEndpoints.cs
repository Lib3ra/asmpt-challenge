using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using OrderManagement.Api.Common.Validation;
using OrderManagement.Api.Features.Boards;
using OrderManagement.Api.Features.Components;
using OrderManagement.Api.Features.Orders;

namespace OrderManagement.Api.Features.Search;

public static class SearchEndpoints
{
    public const string Route = "/api/search";

    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Route, SearchAsync)
            .WithTags("Search")
            .WithSummary("Search orders, boards and components by name")
            .WithDescription("'term' is required and matches case-insensitively anywhere in the name. The optional "
                + "'type' (orders, boards, components) narrows the search to one record type. Results are grouped by "
                + "type and ordered like the list endpoints.");
        return app;
    }

    private static async Task<Results<Ok<SearchResponse>, ValidationProblem>> SearchAsync(
        [AsParameters] SearchRequest request,
        IValidator<SearchRequest> searchValidator,
        IOrderRepository orderRepository,
        IBoardRepository boardRepository,
        IComponentRepository componentRepository,
        CancellationToken cancellationToken)
    {
        ValidationResult validation = await searchValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToProblemErrors());
        }

        string term = request.NormalizedTerm;
        // Each type uses its list endpoint's search, so matching and ordering stay the same everywhere.
        List<SearchHit> orders = request.Includes(SearchType.Orders)
            ? (await orderRepository.SearchOrdersAsync(term, cancellationToken))
                .Select(order => new SearchHit(order.Id, order.Name, order.Description)).ToList()
            : [];
        List<SearchHit> boards = request.Includes(SearchType.Boards)
            ? (await boardRepository.SearchBoardsAsync(term, cancellationToken))
                .Select(board => new SearchHit(board.Id, board.Name, board.Description)).ToList()
            : [];
        List<SearchHit> components = request.Includes(SearchType.Components)
            ? (await componentRepository.SearchComponentsAsync(term, cancellationToken))
                .Select(component => new SearchHit(component.Id, component.Name, component.Description)).ToList()
            : [];
        return TypedResults.Ok(new SearchResponse(orders, boards, components));
    }
}
