using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Application.Auth.Commands.Login;
using SmartShop.Application.Auth.Commands.PromoteToAdmin;
using SmartShop.Application.Auth.Commands.Register;

namespace SmartShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPut("{id}/promote")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PromoteToAdmin(Guid id)
    {
        await _mediator.Send(new PromoteToAdminCommand(id));
        return NoContent();
    }
}