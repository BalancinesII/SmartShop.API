using MediatR;
using SmartShop.Application.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces.Repositories;
using SmartShop.Domain.Interfaces.Services;

namespace SmartShop.Application.Products.Commands.GenerateDescription;

public class GenerateDescriptionHandler : IRequestHandler<GenerateDescriptionCommand, string>
{
    private readonly IProductRepository _productRepository;
    private readonly IAIService _aiService;

    public GenerateDescriptionHandler(IProductRepository productRepository,
                                       IAIService aiService)
    {
        _productRepository = productRepository;
        _aiService = aiService;
    }

    public async Task<string> Handle(GenerateDescriptionCommand request,
                                      CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        var description = await _aiService.GenerateProductDescriptionAsync(
            product.Name,
            product.Category,
            product.Price);

        product.UpdateDescription(description);
        await _productRepository.UpdateAsync(product);

        return description;
    }
}