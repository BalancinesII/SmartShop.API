using MediatR;
using SmartShop.Application.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces.Repositories;

namespace SmartShop.Application.Products.Commands.DeleteProduct;

public class DeleteProductHandler : IRequestHandler<DeleteProductCommand>
{
    private readonly IProductRepository _productRepository;

    public DeleteProductHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var exists = await _productRepository.ExistsAsync(request.Id);

        if (!exists)
            throw new NotFoundException(nameof(Product), request.Id);

        await _productRepository.DeleteAsync(request.Id);
    }
}