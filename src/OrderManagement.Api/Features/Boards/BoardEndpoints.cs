using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Api.Common.Endpoints;
using OrderManagement.Api.Common.Validation;
using OrderManagement.Api.Features.Components;

namespace OrderManagement.Api.Features.Boards;

public static class BoardEndpoints
{
    public const string Route = "/api/boards";
    private const string EntityName = "board";
    private const string ComponentIdsField = "componentIds";

    public static IEndpointRouteBuilder MapBoardEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(Route).WithTags("Boards");
        group.MapGet("/", SearchAsync)
            .WithSummary("List boards with their components")
            .WithDescription("Ordered by name. The optional 'name' filter matches case-insensitively anywhere in the name.");
        group.MapGet("/{id:int}", GetByIdAsync)
            .WithSummary("Get one board with its components");
        group.MapPost("/", CreateAsync)
            .WithSummary("Create one board")
            .WithDescription("Name: required, max 256, unique ignoring case. Description: optional, trimmed, max 2000. "
                + "lengthMm / widthMm: millimetres, greater than 0, at most 1,000, at most two decimal places. "
                + "componentIds: at least one existing component, each at most once.");
        group.MapPut("/{id:int}", UpdateAsync)
            .WithSummary("Update one board")
            .WithDescription("Same rules as create. componentIds replaces the board's components.");
        group.MapDelete("/", DeleteAsync)
            .WithSummary("Delete one or more boards")
            .WithDescription("Pass the ids as '?ids=1&ids=2'. All-or-nothing: if any id is unknown (404), nothing is deleted. "
                + "The links to components are removed; the components remain.");
        return app;
    }

    private static async Task<Ok<List<BoardResponse>>> SearchAsync(
        string? name, IBoardRepository boardRepository, CancellationToken cancellationToken)
    {
        IReadOnlyList<Board> boards = await boardRepository.SearchBoardsAsync(name, cancellationToken);
        return TypedResults.Ok(boards.Select(BoardResponse.From).ToList());
    }

    private static async Task<Results<Ok<BoardResponse>, NotFound>> GetByIdAsync(
        int id, IBoardRepository boardRepository, CancellationToken cancellationToken)
    {
        Board? board = await boardRepository.GetBoardByIdAsync(id, cancellationToken);
        return board is null ? TypedResults.NotFound() : TypedResults.Ok(BoardResponse.From(board));
    }

    private static async Task<Results<Created<BoardResponse>, ValidationProblem>> CreateAsync(
        BoardRequest request,
        IValidator<BoardRequest> boardValidator,
        IBoardRepository boardRepository,
        IComponentRepository componentRepository,
        CancellationToken cancellationToken)
    {
        // Step 1: rules that need no database. Invalid requests stop here without a database query.
        ValidationResult validation = await boardValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToProblemErrors());
        }

        // Step 2: unique name and existing components.
        Dictionary<string, string[]> databaseErrors = await CheckAgainstDatabaseAsync(
            request, excludeId: null, boardRepository, componentRepository, cancellationToken);
        if (databaseErrors.Count > 0)
        {
            return TypedResults.ValidationProblem(databaseErrors);
        }

        Board board = await boardRepository.CreateBoardAsync(request.ToBoard(), request.ComponentIdList, cancellationToken);
        return TypedResults.Created($"{Route}/{board.Id}", BoardResponse.From(board));
    }

    private static async Task<Results<Ok<BoardResponse>, NotFound, ValidationProblem>> UpdateAsync(
        int id,
        BoardRequest request,
        IValidator<BoardRequest> boardValidator,
        IBoardRepository boardRepository,
        IComponentRepository componentRepository,
        CancellationToken cancellationToken)
    {
        if (await boardRepository.GetBoardByIdAsync(id, cancellationToken) is null)
        {
            return TypedResults.NotFound();
        }

        // Step 1: rules that need no database. Invalid requests stop here without a database query.
        ValidationResult validation = await boardValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToProblemErrors());
        }

        // Step 2: unique name and existing components.
        Dictionary<string, string[]> databaseErrors = await CheckAgainstDatabaseAsync(
            request, excludeId: id, boardRepository, componentRepository, cancellationToken);
        if (databaseErrors.Count > 0)
        {
            return TypedResults.ValidationProblem(databaseErrors);
        }

        Board? board = await boardRepository.UpdateBoardAsync(request.ToBoard(id), request.ComponentIdList, cancellationToken);
        return board is null ? TypedResults.NotFound() : TypedResults.Ok(BoardResponse.From(board));
    }

    /// <summary>Deletes one or more boards (<c>?ids=1&amp;ids=2</c>). All-or-nothing: unknown ids (404) delete nothing.</summary>
    private static async Task<Results<NoContent, NotFound<ProblemDetails>, ValidationProblem>> DeleteAsync(
        int[]? ids, IBoardRepository boardRepository, CancellationToken cancellationToken)
    {
        if (ids is null || ids.Length == 0)
        {
            return ProblemResults.IdsRequired();
        }

        List<int> distinctIds = ids.Distinct().ToList();
        IReadOnlyList<int> missingIds = await boardRepository.FindMissingBoardIdsAsync(distinctIds, cancellationToken);
        if (missingIds.Count > 0)
        {
            return ProblemResults.IdsNotFound(EntityName, missingIds);
        }

        await boardRepository.DeleteBoardsAsync(distinctIds, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Dictionary<string, string[]>> CheckAgainstDatabaseAsync(
        BoardRequest request,
        int? excludeId,
        IBoardRepository boardRepository,
        IComponentRepository componentRepository,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string[]> errors = [];
        if (await boardRepository.NameExistsAsync(request.NormalizedName, excludeId, cancellationToken))
        {
            errors["name"] = [$"A {EntityName} named '{request.NormalizedName}' already exists."];
        }

        IReadOnlyList<int> missingComponentIds = await componentRepository.FindMissingComponentIdsAsync(
            request.ComponentIdList.ToList(), cancellationToken);
        if (missingComponentIds.Count > 0)
        {
            errors[ComponentIdsField] = [$"No component exists with id {string.Join(", ", missingComponentIds)}."];
        }

        return errors;
    }
}
