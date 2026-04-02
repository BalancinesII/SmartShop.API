using MediatR;
using SmartShop.Application.Common.DTOs;

namespace SmartShop.Application.Products.Commands.CreateProduct;

public record CreateProductCommand(
    string Name,
    decimal Price,
    int Stock,
    string Category) : IRequest<ProductDto>;