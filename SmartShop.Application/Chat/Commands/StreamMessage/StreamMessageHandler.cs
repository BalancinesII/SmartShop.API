using System.Runtime.CompilerServices;
using System.Text;
using MediatR;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces.Repositories;
using SmartShop.Domain.Interfaces.Services;

namespace SmartShop.Application.Chat.Commands.StreamMessage;

public class StreamMessageHandler : IStreamRequestHandler<StreamMessageCommand, string>
{
    private readonly IChatRepository _chatRepository;
    private readonly IProductRepository _productRepository;
    private readonly IAIService _aiService;

    public StreamMessageHandler(IChatRepository chatRepository,
                                IProductRepository productRepository,
                                IAIService aiService)
    {
        _chatRepository = chatRepository;
        _productRepository = productRepository;
        _aiService = aiService;
    }

    public async IAsyncEnumerable<string> Handle(StreamMessageCommand request,
                                                 [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var history = await _chatRepository.GetSessionMessagesAsync(request.SessionId);
        var products = await _productRepository.GetAllAsync();

        var userMessage = ChatMessage.Create(request.SessionId, "user", request.Message);
        await _chatRepository.AddMessageAsync(userMessage);

        var fullResponse = new StringBuilder();

        await foreach (var fragment in _aiService.StreamChatMessageAsync(
                           request.Message, history, products, cancellationToken))
        {
            fullResponse.Append(fragment);
            yield return fragment;
        }

        // Only a complete answer reaches the history: if the stream fails or the
        // client disconnects, the enumeration stops before this line.
        var assistantMessage = ChatMessage.Create(request.SessionId, "assistant", fullResponse.ToString());
        await _chatRepository.AddMessageAsync(assistantMessage);
    }
}
