using System.Net;
using System.Text.Json;
using FluentAssertions;
using SmartShop.IntegrationTests.Infrastructure;

namespace SmartShop.IntegrationTests.Api;

// El documento OpenAPI y Scalar solo se publican en Development, que es el
// entorno de CustomWebApplicationFactory.
public class OpenApiEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OpenApiEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task OpenApiDocument_DeclaresBearerSecurityScheme()
    {
        // Act
        var response = await _client.GetAsync("/openapi/v1.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        var bearer = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        bearer.GetProperty("type").GetString().Should().Be("http");
        bearer.GetProperty("scheme").GetString().Should().Be("bearer");
        bearer.GetProperty("bearerFormat").GetString().Should().Be("JWT");

        root.GetProperty("security").EnumerateArray()
            .Any(requirement => requirement.TryGetProperty("Bearer", out _))
            .Should().BeTrue();
    }

    [Fact]
    public async Task Scalar_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/scalar");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
