using FluentValidation;
using OrderManagement.Api.Common.Validation;

namespace OrderManagement.Api.Features.Components;

/// <summary>
/// Rules for a bulk create request: at least one item, every item present and valid, no name twice.
/// Errors are keyed per item, e.g. <c>components[1].quantity</c>.
/// </summary>
public sealed class CreateComponentsRequestValidator : AbstractValidator<List<ComponentRequest>>
{
    public const string ListName = "components";

    public CreateComponentsRequestValidator()
    {
        RuleFor(requests => requests)
            .NotEmpty()
            .WithMessage("At least one component is required.")
            .OverridePropertyName(ListName);

        RuleForEach(requests => requests)
            .NotNull()
            .WithMessage("Each component must be an object with a name and a quantity.")
            .SetValidator(new ComponentRequestValidator())
            .OverridePropertyName(ListName);

        RuleFor(requests => requests).UniqueNames(ListName, request => request.Name);
    }
}
