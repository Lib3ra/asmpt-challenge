using FluentValidation.TestHelper;
using OrderManagement.Api.Features.Components;

namespace OrderManagement.Api.Tests.Features.Components;

[TestFixture]
public sealed class CreateComponentsRequestValidatorTests
{
    private readonly CreateComponentsRequestValidator _validator = new();

    private static ComponentRequest Request(string? name, int quantity = 4) => new(name, null, quantity);

    [Test]
    public void Validate_WithEmptyList_ReportsThatOneItemIsRequired()
    {
        // Act
        TestValidationResult<List<ComponentRequest>> result = _validator.TestValidate([]);

        // Assert
        result.ShouldHaveValidationErrorFor("components").WithErrorMessage("At least one component is required.");
    }

    [Test]
    public void Validate_WithInvalidItems_ReportsErrorsPerItemIndex()
    {
        // Arrange
        List<ComponentRequest> requests = [Request("Capacitor"), Request(""), Request("LED", quantity: 0)];

        // Act
        TestValidationResult<List<ComponentRequest>> result = _validator.TestValidate(requests);

        // Assert
        Assert.That(result.Errors.Select(failure => failure.PropertyName),
            Is.EquivalentTo(new[] { "components[1].Name", "components[2].Quantity" }));
    }

    [Test]
    public void Validate_WithSameNameInDifferentCaseAndSpacing_ReportsDuplicateOnLaterItem()
    {
        // Arrange
        List<ComponentRequest> requests = [Request("LED green"), Request("Capacitor"), Request(" led GREEN ")];

        // Act
        TestValidationResult<List<ComponentRequest>> result = _validator.TestValidate(requests);

        // Assert
        result.ShouldHaveValidationErrorFor("components[2].Name")
            .WithErrorMessage("The name 'led GREEN' appears more than once in this request.")
            .Only();
    }
}
