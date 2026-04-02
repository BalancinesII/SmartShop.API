using SmartShop.Domain.Entities;

namespace SmartShop.Domain.Interfaces.Services;

public interface IAIService
{
    Task<string> GenerateProductDescriptionAsync(string productName,
                                                  string category,
                                                  decimal price);

    Task<string> SendChatMessageAsync(string userMessage,
                                       IEnumerable<ChatMessage> history);
}   