using MediatR;

namespace SmartShop.Application.Products.Commands.DeleteProduct;

public record DeleteProductCommand(Guid Id) : IRequest;