using FluentAssertions;
using Moq;
using SmartShop.Application.Common.Exceptions;
using SmartShop.Application.Products.Commands.GenerateDescription;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces.Repositories;
using SmartShop.Domain.Interfaces.Services;

namespace SmartShop.UnitTests.Application.Products;

public class GenerateDescriptionHandlerTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly Mock<IAIService> _aiServiceMock;
    private readonly GenerateDescriptionHandler _handler;

    public GenerateDescriptionHandlerTests()
    {
        _repositoryMock = new Mock<IProductRepository>();
        _aiServiceMock = new Mock<IAIService>();
        _handler = new GenerateDescriptionHandler(_repositoryMock.Object, _aiServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingProduct_ReturnsGeneratedDescription()
    {
        // Arrange
        var product = Product.Create("Zapatillas Pro", 89.99m, 50, "Calzado");
        var expectedDescription = "Descripción generada por IA";

        _repositoryMock
            .Setup(r => r.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        _aiServiceMock
            .Setup(a => a.GenerateProductDescriptionAsync(
                product.Name, product.Category, product.Price))
            .ReturnsAsync(expectedDescription);

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Product>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new GenerateDescriptionCommand(product.Id), CancellationToken.None);

        // Assert
        result.Should().Be(expectedDescription);
        _aiServiceMock.Verify(a => a.GenerateProductDescriptionAsync(
            product.Name, product.Category, product.Price), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistingProduct_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(nonExistingId))
            .ReturnsAsync((Product?)null);

        // Act
        var act = async () => await _handler.Handle(
            new GenerateDescriptionCommand(nonExistingId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}