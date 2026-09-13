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
                Generate an attractive, concise product description for an online store.

                Product: {productName}
                Category: {category}
                Price: {price:C}

                The description should be 2 to 3 sentences, highlight the main benefits,
                and use a persuasive but honest tone.
                Reply with the description only, no titles or extra formatting.
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
    string userMessage,
    IEnumerable<ChatMessage> history,
    IEnumerable<Product> availableProducts)
    {
        var messages = new List<Message>();

        foreach (var msg in history.TakeLast(10))
        {
            var role = msg.Role == "user" ? RoleType.User : RoleType.Assistant;
            messages.Add(new Message(role, msg.Content));
        }

        messages.Add(new Message(RoleType.User, userMessage));

        var productCatalog = availableProducts.Any()
            ? string.Join("\n", availableProducts.Select(p =>
                $"- {p.Name} | Category: {p.Category} | Price: {p.Price:C} | Stock: {p.Stock}"))
            : "No products are available at the moment.";

        var request = new MessageParameters
        {
            Model = Model,
            MaxTokens = 500,
            System = new List<SystemMessage>
        {
            new SystemMessage(
                $"""
                You are the virtual assistant for SmartShop, an online store.
                You help customers with questions about products, orders and returns.
                Always reply in the customer's language.
                Be friendly, concise and helpful. If you don't know something, say so honestly.

                AVAILABLE PRODUCT CATALOG:
                {productCatalog}

                Use this information to answer questions about availability,
                prices and categories. Do not invent products that are not in the catalog.
                """)
        },
            Messages = messages
        };

        var response = await _client.Messages.GetClaudeMessageAsync(request);
        return response.Content.OfType<TextContent>().FirstOrDefault()?.Text ?? string.Empty;
    }
}
