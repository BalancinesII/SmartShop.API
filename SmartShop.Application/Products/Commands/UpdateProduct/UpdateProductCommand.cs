using MediatR;
using SmartShop.Application.Common.DTOs;

namespace SmartShop.Application.Products.Commands.UpdateProduct;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    decimal Price,
    int Stock,
    string Category) : IRequest<ProductDto>;