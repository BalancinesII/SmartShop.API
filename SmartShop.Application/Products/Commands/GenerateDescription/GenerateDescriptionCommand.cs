using MediatR;

namespace SmartShop.Application.Products.Commands.GenerateDescription;

public record GenerateDescriptionCommand(Guid ProductId) : IRequest<string>;