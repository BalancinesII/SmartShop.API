using MediatR;

namespace SmartShop.Application.Chat.Commands.StreamMessage;

// Same body as SendMessageCommand, but answered as a stream of text fragments.
public record StreamMessageCommand(
    Guid SessionId,
    string Message) : IStreamRequest<string>;
