using FluentAssertions;
using SmartShop.Application.Chat.Commands.SendMessage;
using SmartShop.Application.Chat.Commands.StreamMessage;

namespace SmartShop.UnitTests.Application.Chat;

public class StreamMessageValidatorTests
{
    private readonly StreamMessageValidator _validator = new();

    [Fact]
    public void Validate_ValidMessage_IsValid()
    {
        var result = _validator.Validate(new StreamMessageCommand(Guid.NewGuid(), "Hola, ¿tenéis zapatillas?"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyMessage_IsInvalid()
    {
        var result = _validator.Validate(new StreamMessageCommand(Guid.NewGuid(), ""));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_MessageAtMaxLength_IsValid()
    {
        var message = new string('a', SendMessageValidator.MaxMessageLength);

        var result = _validator.Validate(new StreamMessageCommand(Guid.NewGuid(), message));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MessageTooLong_IsInvalid()
    {
        var longMessage = new string('a', SendMessageValidator.MaxMessageLength + 1);

        var result = _validator.Validate(new StreamMessageCommand(Guid.NewGuid(), longMessage));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_EmptySessionId_IsInvalid()
    {
        var result = _validator.Validate(new StreamMessageCommand(Guid.Empty, "Hola"));

        result.IsValid.Should().BeFalse();
    }
}
