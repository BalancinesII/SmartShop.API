using FluentAssertions;
using Moq;
using SmartShop.Application.Common.Exceptions;
using SmartShop.Application.Payments.Commands.CreateCheckout;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces.Repositories;
using SmartShop.Domain.Interfaces.Services;

namespace SmartShop.UnitTests.Application.Payments;

public class CreateCheckoutHandlerTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly CreateCheckoutHandler _handler;

    public CreateCheckoutHandlerTests()
    {
        _productRepositoryMock = new Mock<IProductRepository>();
        _paymentServiceMock = new Mock<IPaymentService>();
        _handler = new CreateCheckoutHandler(
            _productRepositoryMock.Object,
            _paymentServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingProduct_ReturnsCheckoutUrl()
    {
        // Arrange
        var product = Product.Create("Zapatillas Pro", 89.99m, 50, "Calzado");
        var expectedUrl = "https://checkout.stripe.test/session_123";

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        _paymentServiceMock
            .Setup(p => p.CreateCheckoutSessionAsync(product.Id, product.Name, product.Price, 2))
            .ReturnsAsync(expectedUrl);

        // Act
        var result = await _handler.Handle(
            new CreateCheckoutCommand(product.Id, 2), CancellationToken.None);

        // Assert
        result.CheckoutUrl.Should().Be(expectedUrl);
        _paymentServiceMock.Verify(p => p.CreateCheckoutSessionAsync(
            product.Id, product.Name, product.Price, 2), Times.Once);
    }

    [Fact]
    public async Task Handle_QuantityBelowOne_UsesQuantityOne()
    {
        // Arrange
        var product = Product.Create("Gorra Running", 19.99m, 75, "Accesorios");

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        _paymentServiceMock
            .Setup(p => p.CreateCheckoutSessionAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<int>()))
            .ReturnsAsync("https://checkout.stripe.test/session_456");

        // Act
        await _handler.Handle(
            new CreateCheckoutCommand(product.Id, 0), CancellationToken.None);

        // Assert
        _paymentServiceMock.Verify(p => p.CreateCheckoutSessionAsync(
            product.Id, product.Name, product.Price, 1), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistingProduct_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(nonExistingId))
            .ReturnsAsync((Product?)null);

        // Act
        var act = async () => await _handler.Handle(
            new CreateCheckoutCommand(nonExistingId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Product with id '{nonExistingId}' was not found.");
        _paymentServiceMock.Verify(p => p.CreateCheckoutSessionAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<int>()), Times.Never);
    }
}
