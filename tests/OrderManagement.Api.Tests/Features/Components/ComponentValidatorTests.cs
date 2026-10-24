using OrderManagement.Api.Features.Components;

namespace OrderManagement.Api.Tests.Features.Components;

[TestFixture]
public sealed class ComponentValidatorTests
{
    private static ComponentRequest ValidRequest(string? name = "Resistor 10 kOhm", int quantity = 4) =>
        new(name, "0603", quantity);

    [Test]
    public void Validate_WithValidRequest_ReturnsNoErrors()
    {
        // Act
        Dictionary<string, string[]> errors = ComponentValidator.Validate(ValidRequest()).ToDictionary();

        // Assert
        Assert.That(errors, Is.Empty);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Validate_WithMissingOrBlankName_ReportsNameRequired(string? name)
    {
        // Act
        Dictionary<string, string[]> errors = ComponentValidator.Validate(ValidRequest(name)).ToDictionary();

        // Assert
        Assert.That(errors["name"], Is.EqualTo(new[] { "Name is required." }));
    }

    [Test]
    public void Validate_WithNameLongerThanLimitAfterTrimming_ReportsMaxLength()
    {
        // Arrange
        string tooLong = new('x', Component.NameMaxLength + 1);

        // Act
        Dictionary<string, string[]> errors = ComponentValidator.Validate(ValidRequest($"  {tooLong}  ")).ToDictionary();

        // Assert
        Assert.That(errors["name"], Is.EqualTo(new[] { "Name must be at most 256 characters." }));
    }

    [Test]
    public void Validate_WithNameAtLimitSurroundedBySpaces_ReturnsNoErrors()
    {
        // Arrange
        string atLimit = new('x', Component.NameMaxLength);

        // Act
        Dictionary<string, string[]> errors = ComponentValidator.Validate(ValidRequest($"  {atLimit}  ")).ToDictionary();

        // Assert
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_WithDescriptionLongerThanLimit_ReportsMaxLength()
    {
        // Arrange
        ComponentRequest request = new("LED", new string('x', Component.DescriptionMaxLength + 1), 1);

        // Act
        Dictionary<string, string[]> errors = ComponentValidator.Validate(request).ToDictionary();

        // Assert
        Assert.That(errors["description"], Is.EqualTo(new[] { "Description must be at most 2000 characters." }));
    }

    [Test]
    public void Validate_WithDescriptionAtLimitSurroundedBySpaces_ReturnsNoErrors()
    {
        // Arrange
        ComponentRequest request = new("LED", $"  {new string('x', Component.DescriptionMaxLength)}  ", 1);

        // Act
        Dictionary<string, string[]> errors = ComponentValidator.Validate(request).ToDictionary();

        // Assert
        Assert.That(errors, Is.Empty);
    }

    [TestCase(1)]
    [TestCase(10000)]
    public void Validate_WithQuantityAtBoundary_ReturnsNoErrors(int quantity)
    {
        // Act
        Dictionary<string, string[]> errors = ComponentValidator.Validate(ValidRequest(quantity: quantity)).ToDictionary();

        // Assert
        Assert.That(errors, Is.Empty);
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(10_001)]
    public void Validate_WithQuantityOutOfRange_ReportsRange(int quantity)
    {
        // Act
        Dictionary<string, string[]> errors = ComponentValidator.Validate(ValidRequest(quantity: quantity)).ToDictionary();

        // Assert
        Assert.That(errors["quantity"], Is.EqualTo(new[] { "Quantity must be between 1 and 10,000." }));
    }

    [Test]
    public void ValidateAll_WithEmptyList_ReportsThatOneItemIsRequired()
    {
        // Act
        Dictionary<string, string[]> errors = ComponentValidator.ValidateAll([]).ToDictionary();

        // Assert
        Assert.That(errors[""], Is.EqualTo(new[] { "At least one component is required." }));
    }

    [Test]
    public void ValidateAll_WithInvalidItems_ReportsErrorsPerItemIndex()
    {
        // Arrange
        List<ComponentRequest> requests = [ValidRequest("Capacitor"), ValidRequest(name: ""), ValidRequest("LED", quantity: 0)];

        // Act
        Dictionary<string, string[]> errors = ComponentValidator.ValidateAll(requests).ToDictionary();

        // Assert
        Assert.That(errors.Keys, Is.EquivalentTo(new[] { "[1].name", "[2].quantity" }));
    }

    [Test]
    public void ValidateAll_WithSameNameInDifferentCaseAndSpacing_ReportsDuplicateOnLaterItem()
    {
        // Arrange
        List<ComponentRequest> requests = [ValidRequest("LED green"), ValidRequest("Capacitor"), ValidRequest(" led GREEN ")];

        // Act
        Dictionary<string, string[]> errors = ComponentValidator.ValidateAll(requests).ToDictionary();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(errors.Keys, Is.EquivalentTo(new[] { "[2].name" }));
            Assert.That(errors["[2].name"], Is.EqualTo(new[] { "The name 'led GREEN' appears more than once in this request." }));
        }
    }
}
