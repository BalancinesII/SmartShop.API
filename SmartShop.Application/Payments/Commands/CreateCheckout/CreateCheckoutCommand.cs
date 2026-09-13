using MediatR;

namespace SmartShop.Application.Payments.Commands.CreateCheckout;

public record CreateCheckoutCommand(Guid ProductId, int Quantity = 1)
    : IRequest<CreateCheckoutResult>;

public record CreateCheckoutResult(string CheckoutUrl);
