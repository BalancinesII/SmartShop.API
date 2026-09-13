using MediatR;
using Microsoft.AspNetCore.Identity;
using SmartShop.Application.Auth.Commands.Login;
using SmartShop.Domain.Interfaces.Repositories;
using SmartShop.Domain.Interfaces.Services;

namespace SmartShop.Application.Auth.Commands.Login;

public class LoginHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher<object> _passwordHasher;

    public LoginHandler(IUserRepository userRepository,
                        IJwtService jwtService,
                        IPasswordHasher<object> passwordHasher)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request,
                                               CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        var result = _passwordHasher.VerifyHashedPassword(
            new object(), user.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid credentials.");

        return new AuthResponseDto
        {
            Token = _jwtService.GenerateToken(user),
            Email = user.Email,
            Role = user.Role
        };
    }
}