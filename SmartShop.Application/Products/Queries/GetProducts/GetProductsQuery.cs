using MediatR;
using SmartShop.Application.Common.DTOs;

namespace SmartShop.Application.Products.Queries.GetProducts;

public record GetProductsQuery(int PageNumber = 1, int PageSize = 10) : IRequest<PagedResult<ProductDto>>;