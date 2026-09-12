using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using SmartShop.Application.Auth.Commands.Login;
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

    private async Task AuthenticateAsAdminAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/Auth/login",
            new LoginCommand(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword));

        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", result!.Token);
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
        await AuthenticateAsAdminAsync();
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
        await AuthenticateAsAdminAsync();
        var command = new CreateProductCommand("Gorra Running", 19.99m, 75, "Accesorios");

        // Act
        var response = await _client.PostAsJsonAsync("/api/Products", command);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();

        // Assert
        product!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateProduct_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var command = new CreateProductCommand("Producto sin auth", 9.99m, 5, "Test");

        // Act
        var response = await _client.PostAsJsonAsync("/api/Products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}