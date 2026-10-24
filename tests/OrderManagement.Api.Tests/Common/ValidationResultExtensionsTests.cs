using FluentValidation.Results;
using OrderManagement.Api.Common.Validation;

namespace OrderManagement.Api.Tests.Common;

[TestFixture]
public sealed class ValidationResultExtensionsTests
{
    [Test]
    public void ToProblemErrors_ConvertsPropertyPathsToJsonCasingAndGroupsMessages()
    {
        // Arrange
        ValidationResult result = new([
            new ValidationFailure("components[1].Quantity", "Quantity must be between 1 and 10,000."),
            new ValidationFailure("Name", "Name is required."),
            new ValidationFailure("Name", "Second message."),
        ]);

        // Act
        Dictionary<string, string[]> errors = result.ToProblemErrors();

        // Assert
        Assert.That(errors, Is.EquivalentTo(new Dictionary<string, string[]>
        {
            ["components[1].quantity"] = ["Quantity must be between 1 and 10,000."],
            ["name"] = ["Name is required.", "Second message."],
        }));
    }
}
