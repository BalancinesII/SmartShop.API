using MediatR;
using Microsoft.AspNetCore.Identity;
using SmartShop.Application.Auth.Commands.Login;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces.Repositories;
using SmartShop.Domain.Interfaces.Services;

namespace SmartShop.Application.Auth.Commands.Register;

public class RegisterHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher<object> _passwordHasher;

    public RegisterHandler(IUserRepository userRepository,
                           IJwtService jwtService,
                           IPasswordHasher<object> passwordHasher)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request,
                                               CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsAsync(request.Email))
            throw new InvalidOperationException("This email is already registered.");

        var passwordHash = _passwordHasher.HashPassword(new object(), request.Password);

        var user = User.Create(request.Email, passwordHash,
                               request.FirstName, request.LastName);

        await _userRepository.AddAsync(user);

        return new AuthResponseDto
        {
            Token = _jwtService.GenerateToken(user),
            Email = user.Email,
            Role = user.Role
        };
    }
}