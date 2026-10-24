using Microsoft.AspNetCore.Mvc;

namespace OrderManagement.Api.Features.Search;

/// <summary>The record types the global search can be narrowed to.</summary>
public enum SearchType
{
    Orders,
    Boards,
    Components,
}

/// <summary>
/// Query of <c>GET /api/search?term=led&amp;type=boards</c>; without a type all record types are searched.
/// The type is bound as text so an unknown value gets a validation message instead of a binding error.
/// </summary>
public sealed record SearchRequest(
    [property: FromQuery(Name = "term")] string? Term,
    [property: FromQuery(Name = "type")] string? Type)
{
    public string NormalizedTerm => Term?.Trim() ?? string.Empty;

    /// <summary>The requested type, or null for all types. Only valid after validation.</summary>
    public SearchType? ParsedType => string.IsNullOrWhiteSpace(Type) ? null : FindType(Type);

    /// <summary>
    /// Matches a type by name only, ignoring case. Unlike <c>Enum.TryParse</c> it rejects numbers ("7") and
    /// comma-separated lists ("orders,boards").
    /// </summary>
    public static SearchType? FindType(string text) =>
        Enum.GetValues<SearchType>()
            .Select(type => (SearchType?)type)
            .FirstOrDefault(type => string.Equals(type.ToString(), text.Trim(), StringComparison.OrdinalIgnoreCase));

    public bool Includes(SearchType type) => ParsedType is null || ParsedType == type;
}

/// <summary>One matching record. The client opens the matching list page to see or change it.</summary>
public sealed record SearchHit(int Id, string Name, string Description);

/// <summary>Matches grouped by record type; a group is empty when nothing matches or the type was filtered out.</summary>
public sealed record SearchResponse(List<SearchHit> Orders, List<SearchHit> Boards, List<SearchHit> Components);
