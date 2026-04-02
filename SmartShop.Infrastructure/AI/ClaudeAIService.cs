using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces.Services;

namespace SmartShop.Infrastructure.AI;

public class ClaudeAIService : IAIService
{
    private readonly AnthropicClient _client;
    private const string Model = "claude-sonnet-4-5";

    public ClaudeAIService(AnthropicClient client)
    {
        _client = client;
    }

    public async Task<string> GenerateProductDescriptionAsync(
        string productName, string category, decimal price)
    {
        var messages = new List<Message>
        {
            new Message(RoleType.User,
                $"""
                Genera una descripción de producto atractiva y concisa para una tienda online.
                
                Producto: {productName}
                Categoría: {category}
                Precio: {price:C}
                
                La descripción debe tener entre 2 y 3 frases, destacar los beneficios 
                principales y usar un tono persuasivo pero honesto.
                Responde solo con la descripción, sin títulos ni formato adicional.
                """)
        };

        var request = new MessageParameters
        {
            Model = Model,
            MaxTokens = 300,
            Messages = messages
        };

        var response = await _client.Messages.GetClaudeMessageAsync(request);
        return response.Content.OfType<TextContent>().FirstOrDefault()?.Text ?? string.Empty;
    }

    public async Task<string> SendChatMessageAsync(
        string userMessage, IEnumerable<ChatMessage> history)
    {
        var messages = new List<Message>();

        foreach (var msg in history.TakeLast(10))
        {
            var role = msg.Role == "user" ? RoleType.User : RoleType.Assistant;
            messages.Add(new Message(role, msg.Content));
        }

        messages.Add(new Message(RoleType.User, userMessage));

        var request = new MessageParameters
        {
            Model = Model,
            MaxTokens = 500,
            System = new List<SystemMessage>
            {
                new SystemMessage(
                    """
                    Eres el asistente virtual de SmartShop, una tienda online.
                    Ayudas a los clientes con preguntas sobre productos, pedidos y devoluciones.
                    Responde siempre en el idioma del cliente.
                    Sé amable, conciso y útil. Si no sabes algo, dilo con honestidad.
                    """)
            },
            Messages = messages
        };

        var response = await _client.Messages.GetClaudeMessageAsync(request);
        return response.Content.OfType<TextContent>().FirstOrDefault()?.Text ?? string.Empty;
    }
}