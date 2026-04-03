using MediatR;
using SmartShop.Application.Common.DTOs;
using SmartShop.Application.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces.Repositories;

namespace SmartShop.Application.Products.Commands.UpdateProduct;

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;

    public UpdateProductHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request,
                                          CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException(nameof(Product), request.Id);

        product.Update(request.Name, request.Price, request.Stock, request.Category);
        await _productRepository.UpdateAsync(product);

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            Category = product.Category,
            IsActive = product.IsActive
        };
    }
}