using SmartShop.Domain.Entities;

namespace SmartShop.Domain.Interfaces.Repositories;

public interface IChatRepository
{
    Task<IEnumerable<ChatMessage>> GetSessionMessagesAsync(Guid sessionId);
    Task AddMessageAsync(ChatMessage message);
}