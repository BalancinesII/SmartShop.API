using MediatR;
using SmartShop.Application.Auth.Commands.Login;

namespace SmartShop.Application.Auth.Commands.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName) : IRequest<AuthResponseDto>;