using FluentValidation.TestHelper;
using OrderManagement.Api.Features.Orders;

namespace OrderManagement.Api.Tests.Features.Orders;

[TestFixture]
public sealed class OrderRequestValidatorTests
{
    private readonly OrderRequestValidator _validator = new();

    private static OrderRequest ValidRequest() => new("ORD-2026-001", "Prototype run", new DateOnly(2026, 10, 24), [1]);

    [Test]
    public void Validate_WithValidRequest_HasNoErrors()
    {
        // Act
        TestValidationResult<OrderRequest> result = _validator.TestValidate(ValidRequest());

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_WithoutOrderDate_ReportsDateRequired()
    {
        // Act
        TestValidationResult<OrderRequest> result = _validator.TestValidate(ValidRequest() with { OrderDate = null });

        // Assert
        result.ShouldHaveValidationErrorFor(request => request.OrderDate).WithErrorMessage("Order date is required.").Only();
    }

    [Test]
    public void Validate_WithBlankName_ReportsNameRequired()
    {
        // Act
        TestValidationResult<OrderRequest> result = _validator.TestValidate(ValidRequest() with { Name = "" });

        // Assert
        result.ShouldHaveValidationErrorFor(request => request.Name).WithErrorMessage("Name is required.");
    }

    [Test]
    public void Validate_WithoutBoards_ReportsThatOneIsRequired()
    {
        // Act
        TestValidationResult<OrderRequest> result = _validator.TestValidate(ValidRequest() with { BoardIds = [] });

        // Assert
        result.ShouldHaveValidationErrorFor(request => request.BoardIds).WithErrorMessage("An order needs at least one board.");
    }

    [Test]
    public void Validate_WithSameBoardTwice_ReportsDuplicate()
    {
        // Act
        TestValidationResult<OrderRequest> result = _validator.TestValidate(ValidRequest() with { BoardIds = [2, 2] });

        // Assert
        result.ShouldHaveValidationErrorFor(request => request.BoardIds).WithErrorMessage("Each board can be assigned only once.");
    }
}
