using FluentAssertions;
using Moq;
using SmartShop.Application.Chat.Commands.StreamMessage;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces.Repositories;
using SmartShop.Domain.Interfaces.Services;

namespace SmartShop.UnitTests.Application.Chat;

public class StreamMessageHandlerTests
{
    private readonly Mock<IChatRepository> _chatRepositoryMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IAIService> _aiServiceMock;
    private readonly List<ChatMessage> _savedMessages = new();
    private readonly StreamMessageHandler _handler;

    public StreamMessageHandlerTests()
    {
        _chatRepositoryMock = new Mock<IChatRepository>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _aiServiceMock = new Mock<IAIService>();

        _chatRepositoryMock
            .Setup(r => r.GetSessionMessagesAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<ChatMessage>());

        _productRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Product>());

        _chatRepositoryMock
            .Setup(r => r.AddMessageAsync(It.IsAny<ChatMessage>()))
            .Callback<ChatMessage>(_savedMessages.Add)
            .Returns(Task.CompletedTask);

        _handler = new StreamMessageHandler(
            _chatRepositoryMock.Object,
            _productRepositoryMock.Object,
            _aiServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidMessage_YieldsEveryFragmentInOrder()
    {
        // Arrange
        string[] fragments = ["Yes, ", "we have ", "running sneakers."];
        SetupStream(Fragments(fragments));

        // Act
        var result = await ReadAllAsync(new StreamMessageCommand(Guid.NewGuid(), "Sneakers?"));

        // Assert
        result.Should().Equal(fragments);
    }

    [Fact]
    public async Task Handle_StreamCompletes_SavesUserMessageAndFullAssistantMessage()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        SetupStream(Fragments("Hello", ", how ", "can I help?"));

        // Act
        await ReadAllAsync(new StreamMessageCommand(sessionId, "Hi"));

        // Assert
        _savedMessages.Should().HaveCount(2);
        _savedMessages.Should().OnlyContain(m => m.SessionId == sessionId);
        _savedMessages[0].Role.Should().Be("user");
        _savedMessages[0].Content.Should().Be("Hi");
        _savedMessages[1].Role.Should().Be("assistant");
        _savedMessages[1].Content.Should().Be("Hello, how can I help?");
    }

    [Fact]
    public async Task Handle_StreamFails_DoesNotSaveAssistantMessage()
    {
        // Arrange
        SetupStream(FailAfterFirstFragment());

        // Act
        var act = () => ReadAllAsync(new StreamMessageCommand(Guid.NewGuid(), "Hi"));

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
        _savedMessages.Should().ContainSingle().Which.Role.Should().Be("user");
    }

    [Fact]
    public async Task Handle_ConsumerStopsEarly_DoesNotSaveAssistantMessage()
    {
        // Arrange — simula que el cliente cierra el chat a mitad de respuesta
        SetupStream(Fragments("one", "two", "three"));

        // Act
        await foreach (var _ in _handler.Handle(new StreamMessageCommand(Guid.NewGuid(), "Hi"), CancellationToken.None))
            break;

        // Assert
        _savedMessages.Should().ContainSingle().Which.Role.Should().Be("user");
    }

    private void SetupStream(IAsyncEnumerable<string> stream) =>
        _aiServiceMock
            .Setup(a => a.StreamChatMessageAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<IEnumerable<Product>>(),
                It.IsAny<CancellationToken>()))
            .Returns(stream);

    private async Task<List<string>> ReadAllAsync(StreamMessageCommand command)
    {
        var result = new List<string>();
        await foreach (var fragment in _handler.Handle(command, CancellationToken.None))
            result.Add(fragment);
        return result;
    }

    private static async IAsyncEnumerable<string> Fragments(params string[] fragments)
    {
        foreach (var fragment in fragments)
        {
            await Task.Yield();
            yield return fragment;
        }
    }

    private static async IAsyncEnumerable<string> FailAfterFirstFragment()
    {
        await Task.Yield();
        yield return "Partial ";
        throw new HttpRequestException("Simulated AI provider failure.");
    }
}
