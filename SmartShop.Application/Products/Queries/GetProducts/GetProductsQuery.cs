using MediatR;
using SmartShop.Application.Common.DTOs;

namespace SmartShop.Application.Products.Queries.GetProducts;

public record GetProductsQuery() : IRequest<IEnumerable<ProductDto>>;