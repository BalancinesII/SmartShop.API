using SmartShop.Domain.Entities;

namespace SmartShop.Domain.Interfaces.Services;

public interface IAIService
{
    Task<string> GenerateProductDescriptionAsync(string productName,
                                                  string category,
                                                  decimal price);

    Task<string> SendChatMessageAsync(string userMessage,
                                       IEnumerable<ChatMessage> history,
                                       IEnumerable<Product> availableProducts);

    // Same conversation as SendChatMessageAsync, but yields the reply in text
    // fragments as the model generates them.
    IAsyncEnumerable<string> StreamChatMessageAsync(string userMessage,
                                                    IEnumerable<ChatMessage> history,
                                                    IEnumerable<Product> availableProducts,
                                                    CancellationToken cancellationToken = default);
}