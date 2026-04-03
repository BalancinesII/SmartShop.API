using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Application.Products.Commands.CreateProduct;
using SmartShop.Application.Products.Commands.DeleteProduct;
using SmartShop.Application.Products.Commands.GenerateDescription;
using SmartShop.Application.Products.Commands.UpdateProduct;
using SmartShop.Application.Products.Queries.GetProducts;

namespace SmartShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _mediator.Send(new GetProductsQuery());
        return Ok(products);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command)
    {
        var product = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductCommand command)
    {
        if (id != command.Id)
            return BadRequest("El id de la ruta no coincide con el del body.");

        var product = await _mediator.Send(command);
        return Ok(product);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteProductCommand(id));
        return NoContent();
    }

    [HttpPost("{id}/describe")]
    [Authorize]
    public async Task<IActionResult> GenerateDescription(Guid id)
    {
        var description = await _mediator.Send(new GenerateDescriptionCommand(id));
        return Ok(new { description });
    }
}