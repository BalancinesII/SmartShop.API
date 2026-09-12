using MediatR;
using SmartShop.Application.Common.Exceptions;
using SmartShop.Domain.Interfaces.Repositories;

namespace SmartShop.Application.Auth.Commands.PromoteToAdmin;

public class PromoteToAdminHandler : IRequestHandler<PromoteToAdminCommand, Unit>
{
    private readonly IUserRepository _userRepository;

    public PromoteToAdminHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Unit> Handle(PromoteToAdminCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), request.UserId);

        user.PromoteToAdmin();
        await _userRepository.UpdateAsync(user);

        return Unit.Value;
    }
}
