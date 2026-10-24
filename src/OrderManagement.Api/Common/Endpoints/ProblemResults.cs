using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace OrderManagement.Api.Common.Endpoints;

/// <summary>Problem details responses shared by the entity endpoints, so all entities answer the same way.</summary>
public static class ProblemResults
{
    public const string IdsField = "ids";

    /// <summary>400 for a delete request without ids.</summary>
    public static ValidationProblem IdsRequired() =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [IdsField] = ["At least one id is required."] });

    /// <summary>404 for a bulk request that references ids that do not exist. Nothing was changed.</summary>
    public static NotFound<ProblemDetails> IdsNotFound(string entityName, IEnumerable<int> missingIds) =>
        TypedResults.NotFound(new ProblemDetails
        {
            Title = "Not found",
            Detail = $"No {entityName} exists with id {string.Join(", ", missingIds)}. Nothing was changed.",
            Status = StatusCodes.Status404NotFound,
        });

    /// <summary>409 for a delete that is blocked because the records are still in use. Nothing was deleted.</summary>
    public static Conflict<ProblemDetails> InUse(string detail) =>
        TypedResults.Conflict(new ProblemDetails
        {
            Title = "Still in use",
            Detail = $"{detail} Nothing was deleted.",
            Status = StatusCodes.Status409Conflict,
        });
}
