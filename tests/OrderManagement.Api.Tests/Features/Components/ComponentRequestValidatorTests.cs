using FluentValidation.TestHelper;
using OrderManagement.Api.Features.Components;

namespace OrderManagement.Api.Tests.Features.Components;

[TestFixture]
public sealed class ComponentRequestValidatorTests
{
    private readonly ComponentRequestValidator _validator = new();

    private static ComponentRequest ValidRequest(string? name = "Resistor 10 kOhm", string? description = "0603", int quantity = 4) =>
        new(name, description, quantity);

    [Test]
    public void Validate_WithValidRequest_HasNoErrors()
    {
        // Act
        TestValidationResult<ComponentRequest> result = _validator.TestValidate(ValidRequest());

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Validate_WithMissingOrBlankName_ReportsNameRequired(string? name)
    {
        // Act
        TestValidationResult<ComponentRequest> result = _validator.TestValidate(ValidRequest(name));

        // Assert
        result.ShouldHaveValidationErrorFor(request => request.Name).WithErrorMessage("Name is required.").Only();
    }

    [Test]
    public void Validate_WithNameLongerThanLimitAfterTrimming_ReportsMaxLength()
    {
        // Arrange
        string tooLong = new('x', Component.NameMaxLength + 1);

        // Act
        TestValidationResult<ComponentRequest> result = _validator.TestValidate(ValidRequest($"  {tooLong}  "));

        // Assert
        result.ShouldHaveValidationErrorFor(request => request.Name).WithErrorMessage("Name must be at most 256 characters.");
    }

    [Test]
    public void Validate_WithNameAndDescriptionAtLimitSurroundedBySpaces_HasNoErrors()
    {
        // Arrange
        string name = $"  {new string('x', Component.NameMaxLength)}  ";
        string description = $"  {new string('x', Component.DescriptionMaxLength)}  ";

        // Act
        TestValidationResult<ComponentRequest> result = _validator.TestValidate(ValidRequest(name, description));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_WithDescriptionLongerThanLimit_ReportsMaxLength()
    {
        // Arrange
        string tooLong = new('x', Component.DescriptionMaxLength + 1);

        // Act
        TestValidationResult<ComponentRequest> result = _validator.TestValidate(ValidRequest(description: tooLong));

        // Assert
        result.ShouldHaveValidationErrorFor(request => request.Description)
            .WithErrorMessage("Description must be at most 2000 characters.");
    }

    [TestCase(1)]
    [TestCase(10000)]
    public void Validate_WithQuantityAtBoundary_HasNoErrors(int quantity)
    {
        // Act
        TestValidationResult<ComponentRequest> result = _validator.TestValidate(ValidRequest(quantity: quantity));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(10001)]
    public void Validate_WithQuantityOutOfRange_ReportsRange(int quantity)
    {
        // Act
        TestValidationResult<ComponentRequest> result = _validator.TestValidate(ValidRequest(quantity: quantity));

        // Assert
        result.ShouldHaveValidationErrorFor(request => request.Quantity)
            .WithErrorMessage("Quantity must be between 1 and 10,000.");
    }
}
