using System.Globalization;
using OrderManagement.Api.Common.Validation;

namespace OrderManagement.Api.Features.Components;

/// <summary>
/// Checks component requests against the domain rules that need no database access.
/// Name uniqueness against stored components is checked by the endpoints through the repository.
/// </summary>
public static class ComponentValidator
{
    public const string QuantityField = "quantity";

    public static ValidationErrors Validate(ComponentRequest request)
    {
        ValidationErrors errors = new();
        EntityRules.ValidateName(request.Name, Component.NameMaxLength, errors);
        EntityRules.ValidateDescription(request.Description, Component.DescriptionMaxLength, errors);

        if (request.Quantity is < Component.MinQuantity or > Component.MaxQuantity)
        {
            // Invariant culture: the message must not depend on the server's locale (10,000 vs 10.000).
            errors.Add(QuantityField, string.Create(CultureInfo.InvariantCulture,
                $"Quantity must be between {Component.MinQuantity} and {Component.MaxQuantity:N0}."));
        }

        return errors;
    }

    /// <summary>Validates a bulk create request: at least one item, every item valid, no repeated names.</summary>
    public static ValidationErrors ValidateAll(IReadOnlyList<ComponentRequest> requests)
    {
        ValidationErrors errors = new();
        if (requests.Count == 0)
        {
            errors.Add(string.Empty, "At least one component is required.");
            return errors;
        }

        for (int index = 0; index < requests.Count; index++)
        {
            errors.AddItem(index, Validate(requests[index]));
        }

        EntityRules.ValidateUniqueNamesInRequest(requests.Select(request => request.Name).ToList(), errors);
        return errors;
    }
}
