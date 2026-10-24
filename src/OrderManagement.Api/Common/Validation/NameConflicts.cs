namespace OrderManagement.Api.Common.Validation;

/// <summary>Builds validation errors for names that already belong to another stored record.</summary>
public static class NameConflicts
{
    /// <param name="entityName">Used in the message, e.g. "component".</param>
    /// <param name="candidates">The JSON field of each name (e.g. <c>name</c>, <c>components[1].name</c>) and the name.</param>
    /// <param name="existingNames">Names already stored, compared case-insensitively by the caller's set.</param>
    public static Dictionary<string, string[]> ToErrors(
        string entityName, IEnumerable<(string Field, string Name)> candidates, IReadOnlySet<string> existingNames) =>
        candidates
            .Where(candidate => existingNames.Contains(candidate.Name))
            .ToDictionary(
                candidate => candidate.Field,
                candidate => new[] { $"A {entityName} named '{candidate.Name}' already exists." });
}
