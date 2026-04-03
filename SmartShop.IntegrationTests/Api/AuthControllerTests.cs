using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartShop.Application.Auth.Commands.Login;
using SmartShop.Application.Auth.Commands.Register;
using SmartShop.IntegrationTests.Infrastructure;

namespace SmartShop.IntegrationTests.Api;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidCommand_ReturnsOkWithToken()
    {
        // Arrange
        var command = new RegisterCommand(
            "test@smartshop.com", "Test123!", "Test", "User");

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/register", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.Email.Should().Be("test@smartshop.com");
        result.Role.Should().Be("Customer");
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var command = new RegisterCommand(
            "duplicate@smartshop.com", "Test123!", "Test", "User");

        // Act
        await _client.PostAsJsonAsync("/api/Auth/register", command);
        var response = await _client.PostAsJsonAsync("/api/Auth/register", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        // Arrange — primero registramos
        var email = "login@smartshop.com";
        var password = "Login123!";

        await _client.PostAsJsonAsync("/api/Auth/register",
            new RegisterCommand(email, password, "Login", "User"));

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/login",
            new LoginCommand(email, password));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        result!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/login",
            new LoginCommand("noexiste@smartshop.com", "wrongpassword"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}