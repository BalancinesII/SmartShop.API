using System.Runtime.CompilerServices;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces.Services;

namespace SmartShop.IntegrationTests.Infrastructure;

// Sustituye a ClaudeAIService en los tests de integración: responde con textos
// fijos y nunca llama a la API de Claude (que cobra por token).
public class FakeAIService : IAIService
{
    public const string ChatResponse = "Fake assistant response.";
    public const string ProductDescription = "Fake product description.";

    // Respuesta en streaming troceada en varios fragmentos, como llegaría de Claude.
    public static readonly string[] StreamedChatFragments = ["Fake ", "streamed ", "assistant ", "response."];

    // Si el mensaje del usuario es este, el stream falla después del primer
    // fragmento, para probar el manejo de errores a mitad de respuesta.
    public const string StreamFailureTrigger = "fail-the-stream";

    public Task<string> GenerateProductDescriptionAsync(string productName,
                                                        string category,
                                                        decimal price) =>
        Task.FromResult(ProductDescription);

    public Task<string> SendChatMessageAsync(string userMessage,
                                             IEnumerable<ChatMessage> history,
                                             IEnumerable<Product> availableProducts) =>
        Task.FromResult(ChatResponse);

    public async IAsyncEnumerable<string> StreamChatMessageAsync(string userMessage,
                                                                 IEnumerable<ChatMessage> history,
                                                                 IEnumerable<Product> availableProducts,
                                                                 [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < StreamedChatFragments.Length; i++)
        {
            if (i == 1 && userMessage == StreamFailureTrigger)
                throw new HttpRequestException("Simulated failure while streaming from the AI provider.");

            // Cede el hilo entre fragmentos para simular la llegada asíncrona.
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return StreamedChatFragments[i];
        }
    }
}
