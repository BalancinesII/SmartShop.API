using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartShop.Application.Common.DTOs;
using SmartShop.Application.Products.Commands.CreateProduct;
using SmartShop.IntegrationTests.Infrastructure;

namespace SmartShop.IntegrationTests.Api;

public class ProductsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/Products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateProduct_ValidCommand_Returns201()
    {
        // Arrange
        var command = new CreateProductCommand("Mochila Trail", 49.99m, 30, "Accesorios");

        // Act
        var response = await _client.PostAsJsonAsync("/api/Products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        product.Should().NotBeNull();
        product!.Name.Should().Be("Mochila Trail");
        product.Price.Should().Be(49.99m);
        product.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateProduct_ReturnsProductWithId()
    {
        // Arrange
        var command = new CreateProductCommand("Gorra Running", 19.99m, 75, "Accesorios");

        // Act
        var response = await _client.PostAsJsonAsync("/api/Products", command);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();

        // Assert
        product!.Id.Should().NotBeEmpty();
    }
}