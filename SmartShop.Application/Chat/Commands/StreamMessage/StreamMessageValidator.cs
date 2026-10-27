using FluentValidation;
using SmartShop.Application.Chat.Commands.SendMessage;

namespace SmartShop.Application.Chat.Commands.StreamMessage;

// Same rules as SendMessageValidator: the streamed message reaches Claude's
// prompt just the same, so it shares the length cap that keeps the bill bounded.
public class StreamMessageValidator : AbstractValidator<StreamMessageCommand>
{
    public StreamMessageValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("Session id is required.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(SendMessageValidator.MaxMessageLength)
            .WithMessage($"Message cannot exceed {SendMessageValidator.MaxMessageLength} characters.");
    }
}
