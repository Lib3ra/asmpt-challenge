using FluentValidation.TestHelper;
using OrderManagement.Api.Features.Boards;

namespace OrderManagement.Api.Tests.Features.Boards;

[TestFixture]
public sealed class BoardRequestValidatorTests
{
    private readonly BoardRequestValidator _validator = new();

    private static BoardRequest ValidRequest(decimal lengthMm = 100.5m, decimal widthMm = 80.25m, List<int>? componentIds = null) =>
        new("Controller board", "Rev B", lengthMm, widthMm, componentIds ?? [1, 2]);

    [Test]
    public void Validate_WithValidRequest_HasNoErrors()
    {
        // Act
        TestValidationResult<BoardRequest> result = _validator.TestValidate(ValidRequest());

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_WithBlankName_ReportsNameRequired()
    {
        // Act
        TestValidationResult<BoardRequest> result = _validator.TestValidate(ValidRequest() with { Name = " " });

        // Assert
        result.ShouldHaveValidationErrorFor(request => request.Name).WithErrorMessage("Name is required.");
    }

    [TestCase("0.01")]
    [TestCase("1000")]
    [TestCase("999.99")]
    public void Validate_WithDimensionInRange_HasNoErrors(string value)
    {
        // Arrange
        decimal dimension = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        TestValidationResult<BoardRequest> result = _validator.TestValidate(ValidRequest(dimension, dimension));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestCase("0")]
    [TestCase("-5")]
    [TestCase("1000.01")]
    public void Validate_WithDimensionOutOfRange_ReportsRange(string value)
    {
        // Arrange
        decimal dimension = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        TestValidationResult<BoardRequest> result = _validator.TestValidate(ValidRequest(lengthMm: dimension));

        // Assert
        result.ShouldHaveValidationErrorFor(request => request.LengthMm)
            .WithErrorMessage("Length must be greater than 0 and at most 1,000 mm.")
            .Only();
    }

    [Test]
    public void Validate_WithMoreThanTwoDecimalPlaces_ReportsPrecision()
    {
        // Act
        TestValidationResult<BoardRequest> result = _validator.TestValidate(ValidRequest(widthMm: 80.125m));

        // Assert
        result.ShouldHaveValidationErrorFor(request => request.WidthMm)
            .WithErrorMessage("Width can have at most 2 decimal places.")
            .Only();
    }

    [Test]
    public void Validate_WithoutComponents_ReportsThatOneIsRequired()
    {
        // Act
        TestValidationResult<BoardRequest> nullResult = _validator.TestValidate(ValidRequest() with { ComponentIds = null });
        TestValidationResult<BoardRequest> emptyResult = _validator.TestValidate(ValidRequest(componentIds: []));

        // Assert
        nullResult.ShouldHaveValidationErrorFor(request => request.ComponentIds)
            .WithErrorMessage("A board needs at least one component.");
        emptyResult.ShouldHaveValidationErrorFor(request => request.ComponentIds)
            .WithErrorMessage("A board needs at least one component.");
    }

    [Test]
    public void Validate_WithSameComponentTwice_ReportsDuplicate()
    {
        // Act
        TestValidationResult<BoardRequest> result = _validator.TestValidate(ValidRequest(componentIds: [1, 3, 1]));

        // Assert
        result.ShouldHaveValidationErrorFor(request => request.ComponentIds)
            .WithErrorMessage("Each component can be assigned only once.");
    }
}
