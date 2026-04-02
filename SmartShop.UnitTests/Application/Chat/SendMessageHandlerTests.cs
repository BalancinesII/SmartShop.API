using FluentAssertions;
using Moq;
using SmartShop.Application.Chat.Commands.SendMessage;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces.Repositories;
using SmartShop.Domain.Interfaces.Services;

namespace SmartShop.UnitTests.Application.Chat;

public class SendMessageHandlerTests
{
    private readonly Mock<IChatRepository> _chatRepositoryMock;
    private readonly Mock<IAIService> _aiServiceMock;
    private readonly SendMessageHandler _handler;

    public SendMessageHandlerTests()
    {
        _chatRepositoryMock = new Mock<IChatRepository>();
        _aiServiceMock = new Mock<IAIService>();
        _handler = new SendMessageHandler(_chatRepositoryMock.Object, _aiServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidMessage_ReturnsAIResponse()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var userMessage = "Hola, tenéis zapatillas?";
        var expectedResponse = "Sí, tenemos zapatillas de running.";

        _chatRepositoryMock
            .Setup(r => r.GetSessionMessagesAsync(sessionId))
            .ReturnsAsync(new List<ChatMessage>());

        _chatRepositoryMock
            .Setup(r => r.AddMessageAsync(It.IsAny<ChatMessage>()))
            .Returns(Task.CompletedTask);

        _aiServiceMock
            .Setup(a => a.SendChatMessageAsync(
                userMessage, It.IsAny<IEnumerable<ChatMessage>>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _handler.Handle(
            new SendMessageCommand(sessionId, userMessage), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(sessionId);
        result.Response.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_ValidMessage_SavesBothUserAndAssistantMessages()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _chatRepositoryMock
            .Setup(r => r.GetSessionMessagesAsync(sessionId))
            .ReturnsAsync(new List<ChatMessage>());

        _chatRepositoryMock
            .Setup(r => r.AddMessageAsync(It.IsAny<ChatMessage>()))
            .Returns(Task.CompletedTask);

        _aiServiceMock
            .Setup(a => a.SendChatMessageAsync(
                It.IsAny<string>(), It.IsAny<IEnumerable<ChatMessage>>()))
            .ReturnsAsync("Respuesta del asistente");

        // Act
        await _handler.Handle(
            new SendMessageCommand(sessionId, "Mensaje"), CancellationToken.None);

        // Assert — se guarda mensaje usuario + mensaje asistente = 2 veces
        _chatRepositoryMock.Verify(
            r => r.AddMessageAsync(It.IsAny<ChatMessage>()), Times.Exactly(2));
    }
}