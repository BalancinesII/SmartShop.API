using MediatR;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces.Repositories;
using SmartShop.Domain.Interfaces.Services;

namespace SmartShop.Application.Chat.Commands.SendMessage;

public class SendMessageHandler : IRequestHandler<SendMessageCommand, SendMessageResponseDto>
{
    private readonly IChatRepository _chatRepository;
    private readonly IAIService _aiService;

    public SendMessageHandler(IChatRepository chatRepository, IAIService aiService)
    {
        _chatRepository = chatRepository;
        _aiService = aiService;
    }

    public async Task<SendMessageResponseDto> Handle(SendMessageCommand request,
                                                      CancellationToken cancellationToken)
    {
        // Recuperar historial de la sesión
        var history = await _chatRepository.GetSessionMessagesAsync(request.SessionId);

        // Guardar mensaje del usuario
        var userMessage = ChatMessage.Create(request.SessionId, "user", request.Message);
        await _chatRepository.AddMessageAsync(userMessage);

        // Llamar a Claude con el historial
        var aiResponse = await _aiService.SendChatMessageAsync(request.Message, history);

        // Guardar respuesta del asistente
        var assistantMessage = ChatMessage.Create(request.SessionId, "assistant", aiResponse);
        await _chatRepository.AddMessageAsync(assistantMessage);

        return new SendMessageResponseDto
        {
            SessionId = request.SessionId,
            Response = aiResponse
        };
    }
}