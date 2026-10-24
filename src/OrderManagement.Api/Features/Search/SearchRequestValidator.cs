using FluentValidation;
using OrderManagement.Api.Common.Validation;

namespace OrderManagement.Api.Features.Search;

/// <summary>A search needs a term (an empty search would return every record) and an optional known type.</summary>
public sealed class SearchRequestValidator : AbstractValidator<SearchRequest>
{
    public SearchRequestValidator()
    {
        RuleFor(request => request.Term)
            .Must(term => !string.IsNullOrWhiteSpace(term))
            .WithMessage("Search term is required.")
            .Must(term => (term?.Trim().Length ?? 0) <= EntityLimits.NameMaxLength)
            .WithMessage($"Search term must be at most {EntityLimits.NameMaxLength} characters.");

        RuleFor(request => request.Type)
            .Must(type => string.IsNullOrWhiteSpace(type) || SearchRequest.FindType(type) is not null)
            .WithMessage("Type must be orders, boards or components.");
    }
}
