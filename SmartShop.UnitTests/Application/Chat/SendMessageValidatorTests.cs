using FluentAssertions;
using SmartShop.Application.Chat.Commands.SendMessage;

namespace SmartShop.UnitTests.Application.Chat;

public class SendMessageValidatorTests
{
    private readonly SendMessageValidator _validator = new();

    [Fact]
    public void Validate_ValidMessage_IsValid()
    {
        var result = _validator.Validate(new SendMessageCommand(Guid.NewGuid(), "Hola, ¿tenéis zapatillas?"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyMessage_IsInvalid()
    {
        var result = _validator.Validate(new SendMessageCommand(Guid.NewGuid(), ""));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_MessageTooLong_IsInvalid()
    {
        var longMessage = new string('a', SendMessageValidator.MaxMessageLength + 1);

        var result = _validator.Validate(new SendMessageCommand(Guid.NewGuid(), longMessage));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_EmptySessionId_IsInvalid()
    {
        var result = _validator.Validate(new SendMessageCommand(Guid.Empty, "Hola"));

        result.IsValid.Should().BeFalse();
    }
}
