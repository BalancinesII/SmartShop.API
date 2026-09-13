using Anthropic.SDK;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartShop.Domain.Interfaces.Repositories;
using SmartShop.Domain.Interfaces.Services;
using SmartShop.Infrastructure.AI;
using SmartShop.Infrastructure.Identity;
using SmartShop.Infrastructure.Persistence;
using SmartShop.Infrastructure.Persistence.Repositories;

namespace SmartShop.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core
        services.AddDbContext<SmartShopDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(15),
                    errorNumbersToAdd: null)));

        // Repositorios
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();

        // Claude API
        services.AddSingleton(new AnthropicClient(
            configuration["Claude:ApiKey"] ??
            throw new InvalidOperationException("Claude API key not configured.")));

        services.AddScoped<IAIService, ClaudeAIService>();

        // Identity
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher<object>, PasswordHasher<object>>();

        // Payments (Stripe)
        services.AddScoped<IPaymentService, Payments.StripePaymentService>();

        return services;
    }
}