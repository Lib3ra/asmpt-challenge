using System.Globalization;
using FluentValidation;
using OrderManagement.Api.Common.Validation;

namespace OrderManagement.Api.Features.Components;

/// <summary>
/// Rules for one component that need no database access. Name uniqueness against stored components is
/// checked by the endpoints through the repository.
/// </summary>
public sealed class ComponentRequestValidator : AbstractValidator<ComponentRequest>
{
    public ComponentRequestValidator()
    {
        RuleFor(request => request.Name).EntityName(Component.NameMaxLength);
        RuleFor(request => request.Description).EntityDescription(Component.DescriptionMaxLength);

        // Invariant culture: the message must not depend on the server's locale (10,000 vs 10.000).
        RuleFor(request => request.Quantity)
            .InclusiveBetween(Component.MinQuantity, Component.MaxQuantity)
            .WithMessage(string.Create(CultureInfo.InvariantCulture,
                $"Quantity must be between {Component.MinQuantity} and {Component.MaxQuantity:N0}."));
    }
}
