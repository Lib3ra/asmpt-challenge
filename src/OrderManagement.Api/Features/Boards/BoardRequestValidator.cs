using System.Globalization;
using FluentValidation;
using OrderManagement.Api.Common.Validation;

namespace OrderManagement.Api.Features.Boards;

/// <summary>
/// Rules for one board that need no database access. Name uniqueness and the existence of the referenced
/// components are checked by the endpoints through the repositories.
/// </summary>
public sealed class BoardRequestValidator : AbstractValidator<BoardRequest>
{
    public BoardRequestValidator()
    {
        RuleFor(request => request.Name).EntityName(Board.NameMaxLength);
        RuleFor(request => request.Description).EntityDescription(Board.DescriptionMaxLength);
        RuleFor(request => request.LengthMm).Dimension("Length");
        RuleFor(request => request.WidthMm).Dimension("Width");

        RuleFor(request => request.ComponentIds)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A board needs at least one component.")
            .Must(ids => ids!.Distinct().Count() == ids!.Count)
            .WithMessage("Each component can be assigned only once.");
    }
}

internal static class BoardDimensionRules
{
    /// <summary>Millimetres, greater than 0, at most 1,000, at most two decimal places.</summary>
    public static IRuleBuilderOptions<T, decimal> Dimension<T>(this IRuleBuilderInitial<T, decimal> ruleBuilder, string label) =>
        ruleBuilder
            .Cascade(CascadeMode.Stop)
            .Must(value => value is > 0m and <= Board.MaxDimensionMm)
            .WithMessage(string.Create(CultureInfo.InvariantCulture,
                $"{label} must be greater than 0 and at most {Board.MaxDimensionMm:N0} mm."))
            .Must(value => decimal.Round(value, Board.DimensionDecimals) == value)
            .WithMessage($"{label} can have at most {Board.DimensionDecimals} decimal places.");
}
