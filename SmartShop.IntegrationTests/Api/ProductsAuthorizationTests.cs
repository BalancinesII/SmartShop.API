using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartShop.Application.Common.DTOs;
using SmartShop.Application.Products.Commands.CreateProduct;
using SmartShop.Application.Products.Commands.UpdateProduct;
using SmartShop.IntegrationTests.Infrastructure;

namespace SmartShop.IntegrationTests.Api;

// POST, PUT y DELETE de /api/Products son solo para Admin.
public class ProductsAuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductsAuthorizationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // Construye una petición válida para cada verbo. El id no tiene por qué existir:
    // la autorización se comprueba antes de llegar al handler.
    private static HttpRequestMessage BuildRequest(string method, Guid id, string? token)
    {
        var request = method switch
        {
            "POST" => new HttpRequestMessage(HttpMethod.Post, "/api/Products")
            {
                Content = JsonContent.Create(
                    new CreateProductCommand("Producto Auth", 9.99m, 5, "Test"))
            },
            "PUT" => new HttpRequestMessage(HttpMethod.Put, $"/api/Products/{id}")
            {
                Content = JsonContent.Create(
                    new UpdateProductCommand(id, "Producto Auth", 9.99m, 5, "Test"))
            },
            "DELETE" => new HttpRequestMessage(HttpMethod.Delete, $"/api/Products/{id}"),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Unsupported HTTP method.")
        };

        request.UseBearerToken(token);
        return request;
    }

    private async Task<ProductDto> CreateProductAsAdminAsync(string adminToken)
    {
        var response = await _client.SendAsync(BuildRequest("POST", Guid.Empty, adminToken));
        response.EnsureSuccessStatusCode();

        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        return product!;
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task WriteEndpoints_WithoutToken_ReturnUnauthorized(string method)
    {
        // Act
        var response = await _client.SendAsync(BuildRequest(method, Guid.NewGuid(), token: null));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task WriteEndpoints_AsCustomer_ReturnForbidden(string method)
    {
        // Arrange
        var (_, customerToken) = await TestAuth.RegisterCustomerAsync(_client);

        // Act
        var response = await _client.SendAsync(BuildRequest(method, Guid.NewGuid(), customerToken));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_AsAdmin_ReturnsCreated()
    {
        // Arrange
        var adminToken = await TestAuth.GetAdminTokenAsync(_client);

        // Act
        var response = await _client.SendAsync(BuildRequest("POST", Guid.Empty, adminToken));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Update_AsAdmin_ReturnsOk()
    {
        // Arrange
        var adminToken = await TestAuth.GetAdminTokenAsync(_client);
        var product = await CreateProductAsAdminAsync(adminToken);

        // Act
        var response = await _client.SendAsync(BuildRequest("PUT", product.Id, adminToken));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Delete_AsAdmin_ReturnsNoContent()
    {
        // Arrange
        var adminToken = await TestAuth.GetAdminTokenAsync(_client);
        var product = await CreateProductAsAdminAsync(adminToken);

        // Act
        var response = await _client.SendAsync(BuildRequest("DELETE", product.Id, adminToken));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
