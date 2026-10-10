using FluentValidation;

namespace SmartShop.Application.Chat.Commands.SendMessage;

public class SendMessageValidator : AbstractValidator<SendMessageCommand>
{
    // Cada mensaje acaba en el prompt de Claude y se cobra por token:
    // limitar la longitud evita que alguien dispare la factura con textos enormes.
    public const int MaxMessageLength = 1000;

    public SendMessageValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("Session id is required.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(MaxMessageLength)
            .WithMessage($"Message cannot exceed {MaxMessageLength} characters.");
    }
}
