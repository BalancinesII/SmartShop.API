using MediatR;

namespace SmartShop.Application.Auth.Commands.PromoteToAdmin;

public record PromoteToAdminCommand(Guid UserId) : IRequest<Unit>;
