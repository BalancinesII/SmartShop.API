using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces.Services;

namespace SmartShop.IntegrationTests.Infrastructure;

// Sustituye a ClaudeAIService en los tests de integración: responde con textos
// fijos y nunca llama a la API de Claude (que cobra por token).
public class FakeAIService : IAIService
{
    public const string ChatResponse = "Fake assistant response.";
    public const string ProductDescription = "Fake product description.";

    public Task<string> GenerateProductDescriptionAsync(string productName,
                                                        string category,
                                                        decimal price) =>
        Task.FromResult(ProductDescription);

    public Task<string> SendChatMessageAsync(string userMessage,
                                             IEnumerable<ChatMessage> history,
                                             IEnumerable<Product> availableProducts) =>
        Task.FromResult(ChatResponse);
}
