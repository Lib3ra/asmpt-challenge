using FluentValidation.TestHelper;
using OrderManagement.Api.Common.Validation;
using OrderManagement.Api.Features.Search;

namespace OrderManagement.Api.Tests.Features.Search;

[TestFixture]
public sealed class SearchRequestValidatorTests
{
    private readonly SearchRequestValidator _validator = new();

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Validate_WithoutTerm_ReportsTermRequired(string? term)
    {
        // Act
        TestValidationResult<SearchRequest> result = _validator.TestValidate(new SearchRequest(term, null));

        // Assert
        result.ShouldHaveValidationErrorFor(request => request.Term).WithErrorMessage("Search term is required.").Only();
    }

    [Test]
    public void Validate_WithTermLongerThanNameLimit_ReportsMaxLength()
    {
        // Arrange
        string term = new('a', EntityLimits.NameMaxLength + 1);

        // Act
        TestValidationResult<SearchRequest> result = _validator.TestValidate(new SearchRequest(term, "boards"));

        // Assert
        result.ShouldHaveValidationErrorFor(request => request.Term).WithErrorMessage("Search term must be at most 256 characters.");
    }

    [TestCase(null)]
    [TestCase("boards")]
    [TestCase("Orders")]
    public void Validate_WithoutTypeOrWithKnownTypeInAnyCase_HasNoErrors(string? type)
    {
        // Act
        TestValidationResult<SearchRequest> result = _validator.TestValidate(new SearchRequest("led", type));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestCase("machines")]
    [TestCase("7")]
    [TestCase("orders,boards")]
    public void Validate_WithUnknownType_ReportsAllowedTypes(string type)
    {
        // Act
        TestValidationResult<SearchRequest> result = _validator.TestValidate(new SearchRequest("led", type));

        // Assert
        result.ShouldHaveValidationErrorFor(request => request.Type).WithErrorMessage("Type must be orders, boards or components.").Only();
    }
}
