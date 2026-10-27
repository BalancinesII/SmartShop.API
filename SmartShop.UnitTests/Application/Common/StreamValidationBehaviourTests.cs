using FluentAssertions;
using FluentValidation;
using MediatR;
using SmartShop.Application.Chat.Commands.StreamMessage;
using SmartShop.Application.Common.Behaviours;

namespace SmartShop.UnitTests.Application.Common;

public class StreamValidationBehaviourTests
{
    private readonly StreamValidationBehaviour<StreamMessageCommand, string> _behaviour =
        new([new StreamMessageValidator()]);

    [Fact]
    public async Task Handle_InvalidRequest_ThrowsBeforeCallingTheHandler()
    {
        // Arrange
        var handlerCalled = false;
        StreamHandlerDelegate<string> next = () =>
        {
            handlerCalled = true;
            return Fragments("never");
        };

        // Act
        var act = async () =>
        {
            await foreach (var _ in _behaviour.Handle(new StreamMessageCommand(Guid.NewGuid(), ""), next, CancellationToken.None)) { }
        };

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        handlerCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ValidRequest_PassesTheHandlerFragmentsThrough()
    {
        // Arrange
        StreamHandlerDelegate<string> next = () => Fragments("a", "b");

        // Act
        var result = new List<string>();
        await foreach (var fragment in _behaviour.Handle(new StreamMessageCommand(Guid.NewGuid(), "Hi"), next, CancellationToken.None))
            result.Add(fragment);

        // Assert
        result.Should().Equal("a", "b");
    }

    private static async IAsyncEnumerable<string> Fragments(params string[] fragments)
    {
        foreach (var fragment in fragments)
        {
            await Task.Yield();
            yield return fragment;
        }
    }
}
