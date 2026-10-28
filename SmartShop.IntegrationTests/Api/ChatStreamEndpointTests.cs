using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartShop.Application.Chat.Commands.SendMessage;
using SmartShop.Application.Chat.Commands.StreamMessage;
using SmartShop.Infrastructure.Persistence;
using SmartShop.IntegrationTests.Infrastructure;

namespace SmartShop.IntegrationTests.Api;

public class ChatStreamEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string StreamUrl = "/api/Chat/message/stream";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ChatStreamEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task StreamMessage_ValidMessage_StreamsFakeFragmentsInOrderThenDone()
    {
        // Arrange
        var command = new StreamMessageCommand(Guid.NewGuid(), "Do you have any running shoes?");

        // Act
        var response = await _client.PostAsJsonAsync(StreamUrl, command);

        // Assert — los fragmentos vienen del FakeAIService, no de Claude
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");

        var events = await ServerSentEventsReader.ReadAllAsync(response);

        events.Should().HaveCount(FakeAIService.StreamedChatFragments.Length + 1);
        events.SkipLast(1).Should().OnlyContain(e => e.Event == "chunk");
        events.SkipLast(1).Select(e => e.Data.GetProperty("text").GetString())
              .Should().Equal(FakeAIService.StreamedChatFragments);

        var done = events.Last();
        done.Event.Should().Be("done");
        done.Data.GetProperty("sessionId").GetGuid().Should().Be(command.SessionId);
    }

    [Fact]
    public async Task StreamMessage_ValidMessage_SavesFullAssistantMessageInHistory()
    {
        // Arrange
        var command = new StreamMessageCommand(Guid.NewGuid(), "Hello!");

        // Act
        var response = await _client.PostAsJsonAsync(StreamUrl, command);
        await ServerSentEventsReader.ReadAllAsync(response);

        // Assert — en el historial queda la respuesta completa, no los trozos
        var messages = await GetSessionMessagesAsync(command.SessionId);

        messages.Select(m => m.Role).Should().Equal("user", "assistant");
        messages[0].Content.Should().Be(command.Message);
        messages[1].Content.Should().Be(string.Concat(FakeAIService.StreamedChatFragments));
    }

    [Fact]
    public async Task StreamMessage_AIFailsMidStream_SendsErrorEventAndDoesNotSaveAssistantMessage()
    {
        // Arrange
        var command = new StreamMessageCommand(Guid.NewGuid(), FakeAIService.StreamFailureTrigger);

        // Act
        var response = await _client.PostAsJsonAsync(StreamUrl, command);

        // Assert — las cabeceras ya salieron con 200, así que el error llega como evento
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var events = await ServerSentEventsReader.ReadAllAsync(response);

        events.Select(e => e.Event).Should().Equal("chunk", "error");
        events.Last().Data.GetProperty("error").GetString().Should().NotBeNullOrWhiteSpace();

        var messages = await GetSessionMessagesAsync(command.SessionId);
        messages.Should().ContainSingle().Which.Role.Should().Be("user");
    }

    [Fact]
    public async Task StreamMessage_EmptyMessage_ReturnsBadRequest()
    {
        // Arrange
        var command = new StreamMessageCommand(Guid.NewGuid(), "");

        // Act
        var response = await _client.PostAsJsonAsync(StreamUrl, command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StreamMessage_MessageTooLong_ReturnsBadRequestAndSavesNothing()
    {
        // Arrange
        var message = new string('a', SendMessageValidator.MaxMessageLength + 1);
        var command = new StreamMessageCommand(Guid.NewGuid(), message);

        // Act
        var response = await _client.PostAsJsonAsync(StreamUrl, command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        (await GetSessionMessagesAsync(command.SessionId)).Should().BeEmpty();
    }

    private async Task<List<Domain.Entities.ChatMessage>> GetSessionMessagesAsync(Guid sessionId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartShopDbContext>();

        return await db.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }
}

public class ChatStreamRateLimitTests : IClassFixture<LowRateLimitWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ChatStreamRateLimitTests(LowRateLimitWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task StreamMessage_SharesTheAiRateLimitWithTheRegularChat()
    {
        // Arrange — gasta el cupo entre el endpoint normal y el de streaming
        var sessionId = Guid.NewGuid();
        var regular = await _client.PostAsJsonAsync("/api/Chat/message",
            new SendMessageCommand(sessionId, "First"));
        var streamed = await _client.PostAsJsonAsync("/api/Chat/message/stream",
            new StreamMessageCommand(sessionId, "Second"));

        regular.StatusCode.Should().Be(HttpStatusCode.OK);
        streamed.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var rejected = await _client.PostAsJsonAsync("/api/Chat/message/stream",
            new StreamMessageCommand(sessionId, "Third"));

        // Assert
        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
