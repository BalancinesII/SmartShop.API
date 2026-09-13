using MediatR;
using SmartShop.Application.Common.Exceptions;
using SmartShop.Domain.Interfaces.Repositories;
using SmartShop.Domain.Interfaces.Services;

namespace SmartShop.Application.Payments.Commands.CreateCheckout;

public class CreateCheckoutHandler : IRequestHandler<CreateCheckoutCommand, CreateCheckoutResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IPaymentService _paymentService;

    public CreateCheckoutHandler(
        IProductRepository productRepository,
        IPaymentService paymentService)
    {
        _productRepository = productRepository;
        _paymentService = paymentService;
    }

    public async Task<CreateCheckoutResult> Handle(
        CreateCheckoutCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId)
            ?? throw new NotFoundException(nameof(Domain.Entities.Product), request.ProductId);

        var quantity = request.Quantity < 1 ? 1 : request.Quantity;

        var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(
            product.Id,
            product.Name,
            product.Price,
            quantity);

        return new CreateCheckoutResult(checkoutUrl);
    }
}
