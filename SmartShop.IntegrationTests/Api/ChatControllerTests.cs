using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartShop.Application.Chat.Commands.SendMessage;
using SmartShop.IntegrationTests.Infrastructure;

namespace SmartShop.IntegrationTests.Api;

public class ChatControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ChatControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SendMessage_ValidMessage_ReturnsFakeAIResponse()
    {
        // Arrange
        var command = new SendMessageCommand(Guid.NewGuid(), "Do you have any running shoes?");

        // Act
        var response = await _client.PostAsJsonAsync("/api/Chat/message", command);

        // Assert — la respuesta viene del FakeAIService, no de Claude
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SendMessageResponseDto>();
        result.Should().NotBeNull();
        result!.SessionId.Should().Be(command.SessionId);
        result.Response.Should().Be(FakeAIService.ChatResponse);
    }

    [Fact]
    public async Task SendMessage_EmptyMessage_ReturnsBadRequest()
    {
        // Arrange
        var command = new SendMessageCommand(Guid.NewGuid(), "");

        // Act
        var response = await _client.PostAsJsonAsync("/api/Chat/message", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendMessage_MessageTooLong_ReturnsBadRequest()
    {
        // Arrange
        var message = new string('a', SendMessageValidator.MaxMessageLength + 1);
        var command = new SendMessageCommand(Guid.NewGuid(), message);

        // Act
        var response = await _client.PostAsJsonAsync("/api/Chat/message", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
