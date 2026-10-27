using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmartShop.Application.Auth.Commands.Login;
using SmartShop.Infrastructure.Persistence;
using SmartShop.IntegrationTests.Infrastructure;

namespace SmartShop.IntegrationTests.Api;

// PUT /api/Auth/{id}/promote es solo para Admin.
public class PromoteToAdminEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PromoteToAdminEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // La API no devuelve el id del usuario al registrarse: se lee de la base de datos en memoria.
    private Guid GetUserId(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartShopDbContext>();
        return db.Users.Single(u => u.Email == email).Id;
    }

    private Task<HttpResponseMessage> PromoteAsync(Guid userId, string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/Auth/{userId}/promote");
        request.UseBearerToken(token);
        return _client.SendAsync(request);
    }

    [Fact]
    public async Task Promote_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var (email, _) = await TestAuth.RegisterCustomerAsync(_client);

        // Act
        var response = await PromoteAsync(GetUserId(email), token: null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Promote_AsCustomer_ReturnsForbidden()
    {
        // Arrange — un Customer intenta promocionarse a sí mismo
        var (email, customerToken) = await TestAuth.RegisterCustomerAsync(_client);

        // Act
        var response = await PromoteAsync(GetUserId(email), customerToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Promote_AsAdmin_ReturnsNoContentAndUserBecomesAdmin()
    {
        // Arrange
        var (email, _) = await TestAuth.RegisterCustomerAsync(_client);
        var adminToken = await TestAuth.GetAdminTokenAsync(_client);

        // Act
        var response = await PromoteAsync(GetUserId(email), adminToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var login = await _client.PostAsJsonAsync("/api/Auth/login",
            new LoginCommand(email, TestAuth.CustomerPassword));
        var result = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        result!.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task Promote_AsAdmin_NonExistingUser_ReturnsNotFound()
    {
        // Arrange
        var adminToken = await TestAuth.GetAdminTokenAsync(_client);

        // Act
        var response = await PromoteAsync(Guid.NewGuid(), adminToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
