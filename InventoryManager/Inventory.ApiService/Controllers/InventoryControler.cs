using Inventory.ApiService.Entity;
using Inventory.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController(
    ILogger<InventoryController> logger,
    IInventoryService inventoryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Product>> Get([FromQuery] int? id, CancellationToken ct)
    {
        if (id is null || id < 0)
            return BadRequest("id is required and must be >= 0");

        logger.LogInformation("Processing request for inventory {ResourceId}", id);

        var product = await inventoryService.GetInventory(id.Value, ct);

        return Ok(product);
    }
}