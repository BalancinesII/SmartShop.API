using Microsoft.Extensions.Configuration;
using SmartShop.Domain.Interfaces.Services;
using Stripe.Checkout;

namespace SmartShop.Infrastructure.Payments;

public class StripePaymentService : IPaymentService
{
    private readonly IConfiguration _configuration;

    public StripePaymentService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> CreateCheckoutSessionAsync(
        Guid productId,
        string productName,
        decimal price,
        int quantity)
    {
        // Set the key at call time, not in the constructor — this way the app
        // still boots fine when Stripe isn't configured (e.g. AI-only setups),
        // and only fails if someone actually tries to check out.
        Stripe.StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"]
            ?? throw new InvalidOperationException(
                "Stripe secret key not configured. Set 'Stripe:SecretKey' to enable checkout.");

        var frontendUrl = _configuration["Stripe:FrontendBaseUrl"] ?? "http://localhost:4200";

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = $"{frontendUrl}/checkout/success",
            CancelUrl = $"{frontendUrl}/checkout/cancel",
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = quantity,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "eur",
                        // Stripe works in the smallest currency unit (cents).
                        UnitAmount = (long)(price * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = productName
                        }
                    }
                }
            },
            // Carried through to the webhook so we know which product was paid for.
            Metadata = new Dictionary<string, string>
            {
                ["productId"] = productId.ToString()
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return session.Url;
    }
}
