using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartShop.Application.Chat.Commands.SendMessage;
using SmartShop.Application.Chat.Commands.StreamMessage;

namespace SmartShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    // SSE event names the frontend listens for.
    public const string ChunkEvent = "chunk";
    public const string DoneEvent = "done";
    public const string ErrorEvent = "error";

    private readonly IMediator _mediator;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IMediator mediator, ILogger<ChatController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("message")]
    [EnableRateLimiting("ai")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageCommand command)
    {
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    // Server-Sent Events: "chunk" events with text fragments as Claude writes
    // them, then a single "done" (answer saved to the history) or "error".
    [HttpPost("message/stream")]
    [EnableRateLimiting("ai")]
    public async Task<IResult> StreamMessage([FromBody] StreamMessageCommand command,
                                             CancellationToken cancellationToken)
    {
        var fragments = _mediator.CreateStream(command, cancellationToken)
                                 .GetAsyncEnumerator(cancellationToken);

        // Read the first fragment before any SSE header goes out. Validation runs
        // on this first read, so an invalid message still gets a plain 400 from
        // ExceptionMiddleware, exactly like POST /api/Chat/message.
        bool hasFragment;
        try
        {
            hasFragment = await fragments.MoveNextAsync();
        }
        catch
        {
            await fragments.DisposeAsync();
            throw;
        }

        return TypedResults.ServerSentEvents(
            ToServerSentEvents(command.SessionId, fragments, hasFragment, cancellationToken));
    }

    private async IAsyncEnumerable<SseItem<object>> ToServerSentEvents(
        Guid sessionId,
        IAsyncEnumerator<string> fragments,
        bool hasFragment,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using (fragments)
        {
            var failed = false;

            while (hasFragment)
            {
                yield return new SseItem<object>(new ChatStreamChunk(fragments.Current), ChunkEvent);

                try
                {
                    hasFragment = await fragments.MoveNextAsync();
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // The client closed the connection: nobody is left to notify.
                    yield break;
                }
                catch (Exception ex)
                {
                    // The 200 and SSE headers are already sent, so ExceptionMiddleware
                    // can no longer answer with an error status: report it in-stream.
                    _logger.LogError(ex, "Chat stream failed for session {SessionId}", sessionId);
                    failed = true;
                    break;
                }
            }

            yield return failed
                ? new SseItem<object>(new ChatStreamError("The assistant response could not be completed."), ErrorEvent)
                : new SseItem<object>(new ChatStreamDone(sessionId), DoneEvent);
        }
    }
}

public record ChatStreamChunk(string Text);
public record ChatStreamDone(Guid SessionId);
public record ChatStreamError(string Error);
