using FluentValidation;
using OrderManagement.Api.Common.Validation;

namespace OrderManagement.Api.Features.Orders;

/// <summary>
/// Rules for one order that need no database access. Name uniqueness and the existence of the referenced
/// boards are checked by the endpoints through the repositories.
/// </summary>
public sealed class OrderRequestValidator : AbstractValidator<OrderRequest>
{
    public OrderRequestValidator()
    {
        RuleFor(request => request.Name).EntityName(Order.NameMaxLength);
        RuleFor(request => request.Description).EntityDescription(Order.DescriptionMaxLength);

        RuleFor(request => request.OrderDate)
            .NotNull()
            .WithMessage("Order date is required.");

        RuleFor(request => request.BoardIds)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("An order needs at least one board.")
            .Must(ids => ids!.Distinct().Count() == ids!.Count)
            .WithMessage("Each board can be assigned only once.");
    }
}
