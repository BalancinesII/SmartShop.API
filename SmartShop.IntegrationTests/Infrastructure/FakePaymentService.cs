using SmartShop.Domain.Interfaces.Services;

namespace SmartShop.IntegrationTests.Infrastructure;

// Sustituye a StripePaymentService en los tests de integración: devuelve una URL
// de checkout inventada y nunca llama a Stripe.
public class FakePaymentService : IPaymentService
{
    public const string CheckoutUrlPrefix = "https://checkout.fake.test/";

    public Task<string> CreateCheckoutSessionAsync(Guid productId,
                                                   string productName,
                                                   decimal price,
                                                   int quantity) =>
        Task.FromResult($"{CheckoutUrlPrefix}{productId}");
}
