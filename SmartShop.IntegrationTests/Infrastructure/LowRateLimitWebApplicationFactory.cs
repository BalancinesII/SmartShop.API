using Microsoft.AspNetCore.Hosting;

namespace SmartShop.IntegrationTests.Infrastructure;

// Igual que CustomWebApplicationFactory pero con un límite de peticiones a la IA
// muy bajo, para comprobar el rate limiting sin lanzar 20 peticiones por test.
public class LowRateLimitWebApplicationFactory : CustomWebApplicationFactory
{
    public const int AiPermitLimit = 2;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("RateLimiting:AiPermitLimit", AiPermitLimit.ToString());
        builder.UseSetting("RateLimiting:AiWindowSeconds", "3600");
    }
}
