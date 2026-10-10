using FluentAssertions;
using SmartShop.Application.Payments.Commands.CreateCheckout;

namespace SmartShop.UnitTests.Application.Payments;

public class CreateCheckoutValidatorTests
{
    private readonly CreateCheckoutValidator _validator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(CreateCheckoutValidator.MaxQuantity)]
    public void Validate_QuantityInRange_IsValid(int quantity)
    {
        var result = _validator.Validate(new CreateCheckoutCommand(Guid.NewGuid(), quantity));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(CreateCheckoutValidator.MaxQuantity + 1)]
    public void Validate_QuantityOutOfRange_IsInvalid(int quantity)
    {
        var result = _validator.Validate(new CreateCheckoutCommand(Guid.NewGuid(), quantity));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_EmptyProductId_IsInvalid()
    {
        var result = _validator.Validate(new CreateCheckoutCommand(Guid.Empty, 1));

        result.IsValid.Should().BeFalse();
    }
}
