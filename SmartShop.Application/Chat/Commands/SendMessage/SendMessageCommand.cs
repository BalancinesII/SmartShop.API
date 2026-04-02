using MediatR;

namespace SmartShop.Application.Chat.Commands.SendMessage;

public record SendMessageCommand(
    Guid SessionId,
    string Message) : IRequest<SendMessageResponseDto>;

public class SendMessageResponseDto
{
    public Guid SessionId { get; set; }
    public string Response { get; set; } = string.Empty;
}