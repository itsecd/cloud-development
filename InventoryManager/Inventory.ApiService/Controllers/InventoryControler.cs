using Microsoft.AspNetCore.Mvc;
using Inventory.ApiService.Entity;
using Inventory.ApiService.Cache;
using Inventory.ApiService.Messaging;

namespace Inventory.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController(
    IInventoryCache cache,
    IProducerService producerService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Product>> Get([FromQuery] int? id, CancellationToken ct)
    {
        if (id is null || id < 0)
            return BadRequest("id is required and must be >= 0");

        var product = await cache.GetAsync(id.Value, ct);
        await producerService.SendMessage(product);

        return Ok(product);
    }
}