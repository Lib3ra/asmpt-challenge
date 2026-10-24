using OrderManagement.Api.Features.Orders;

namespace OrderManagement.Api.Tests.Features.Orders;

[TestFixture]
public sealed class ProductionFileNameTests
{
    [TestCase(7, "ORD-2026-001 Prototype run", "order-7-ord-2026-001-prototype-run.json")]
    [TestCase(3, "  Größe / Test  ", "order-3-gr-e-test.json")]
    [TestCase(12, "../../etc/passwd", "order-12-etc-passwd.json")]
    [TestCase(5, "***", "order-5.json")]
    public void For_WithOrderName_ReturnsSafeLowerCaseName(int orderId, string orderName, string expected)
    {
        // Act
        string fileName = ProductionFileName.For(orderId, orderName);

        // Assert
        Assert.That(fileName, Is.EqualTo(expected));
    }

    [Test]
    public void For_WithLongName_ShortensNamePartToFiftyCharacters()
    {
        // Arrange
        string orderName = new('a', 300);

        // Act
        string fileName = ProductionFileName.For(1, orderName);

        // Assert
        Assert.That(fileName, Is.EqualTo($"order-1-{new string('a', 50)}.json"));
    }
}
