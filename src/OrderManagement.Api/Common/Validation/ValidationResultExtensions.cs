using System.Text.Json;
using FluentValidation.Results;

namespace OrderManagement.Api.Common.Validation;

public static class ValidationResultExtensions
{
    /// <summary>
    /// Converts failures to the field → messages dictionary of a validation problem, with keys in the JSON
    /// casing of the request: <c>components[1].Quantity</c> becomes <c>components[1].quantity</c>.
    /// </summary>
    public static Dictionary<string, string[]> ToProblemErrors(this ValidationResult result) =>
        result.Errors
            .GroupBy(failure => ToJsonPath(failure.PropertyName))
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray());

    public static string ToJsonPath(string propertyName) =>
        string.Join('.', propertyName.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}
