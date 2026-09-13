using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartShop.Application.Payments.Commands.CreateCheckout;
using Stripe;

namespace SmartShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IMediator mediator,
        IConfiguration configuration,
        ILogger<PaymentsController> logger)
    {
        _mediator = mediator;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Creates a Stripe Checkout session for a product and returns the URL
    /// the frontend should redirect the customer to.
    /// </summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> CreateCheckout([FromBody] CreateCheckoutCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Stripe calls this endpoint after events (e.g. a completed payment).
    /// The signature is verified so only genuine Stripe events are processed.
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var webhookSecret = _configuration["Stripe:WebhookSecret"];

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                webhookSecret);

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                var productId = session?.Metadata.GetValueOrDefault("productId");

                // Payment confirmed. This is where you'd create an order, decrement
                // stock, send a confirmation email, etc. Kept as a log for the
                // boilerplate — wire in your own fulfilment logic here.
                _logger.LogInformation(
                    "Payment completed for product {ProductId}, session {SessionId}",
                    productId, session?.Id);
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Invalid Stripe webhook signature.");
            return BadRequest();
        }
    }
}
