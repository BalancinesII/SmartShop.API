using FluentAssertions;
using Moq;
using SmartShop.Application.Products.Commands.CreateProduct;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces.Repositories;

namespace SmartShop.UnitTests.Application.Products;

public class CreateProductHandlerTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly CreateProductHandler _handler;

    public CreateProductHandlerTests()
    {
        _repositoryMock = new Mock<IProductRepository>();
        _handler = new CreateProductHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsProductDto()
    {
        // Arrange
        var command = new CreateProductCommand("Camiseta Técnica", 29.99m, 100, "Ropa");

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Product>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Camiseta Técnica");
        result.Price.Should().Be(29.99m);
        result.Stock.Should().Be(100);
        result.Category.Should().Be("Ropa");
        result.IsActive.Should().BeTrue();
        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsRepositoryOnce()
    {
        // Arrange
        var command = new CreateProductCommand("Camiseta Técnica", 29.99m, 100, "Ropa");

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Product>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Once);
    }
}