using MediatR;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces.Repositories;
using SmartShop.Domain.Interfaces.Services;

namespace SmartShop.Application.Chat.Commands.SendMessage;

public class SendMessageHandler : IRequestHandler<SendMessageCommand, SendMessageResponseDto>
{
    private readonly IChatRepository _chatRepository;
    private readonly IProductRepository _productRepository;
    private readonly IAIService _aiService;

    public SendMessageHandler(IChatRepository chatRepository,
                               IProductRepository productRepository,
                               IAIService aiService)
    {
        _chatRepository = chatRepository;
        _productRepository = productRepository;
        _aiService = aiService;
    }

    public async Task<SendMessageResponseDto> Handle(SendMessageCommand request,
                                                      CancellationToken cancellationToken)
    {
        var history = await _chatRepository.GetSessionMessagesAsync(request.SessionId);
        var products = await _productRepository.GetAllAsync();

        var userMessage = ChatMessage.Create(request.SessionId, "user", request.Message);
        await _chatRepository.AddMessageAsync(userMessage);

        var aiResponse = await _aiService.SendChatMessageAsync(
            request.Message, history, products);

        var assistantMessage = ChatMessage.Create(request.SessionId, "assistant", aiResponse);
        await _chatRepository.AddMessageAsync(assistantMessage);

        return new SendMessageResponseDto
        {
            SessionId = request.SessionId,
            Response = aiResponse
        };
    }
}