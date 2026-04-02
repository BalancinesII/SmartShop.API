using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Application.Chat.Commands.SendMessage;

namespace SmartShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChatController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("message")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageCommand command)
    {
        var response = await _mediator.Send(command);
        return Ok(response);
    }
}