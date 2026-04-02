using SmartShop.Domain.Entities;

namespace SmartShop.Domain.Interfaces.Services;

public interface IJwtService
{
    string GenerateToken(User user);
}