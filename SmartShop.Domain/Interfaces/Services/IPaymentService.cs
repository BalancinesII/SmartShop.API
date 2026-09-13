namespace SmartShop.Domain.Interfaces.Services;

public interface IPaymentService
{
    /// <summary>
    /// Creates a hosted checkout session for a product and returns the URL
    /// the customer should be redirected to in order to pay. Success/cancel
    /// return URLs are resolved from configuration by the implementation.
    /// </summary>
    Task<string> CreateCheckoutSessionAsync(
        Guid productId,
        string productName,
        decimal price,
        int quantity);
}
