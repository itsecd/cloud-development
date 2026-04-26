using Inventory.ApiService.Entity;
using Inventory.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.ApiService.Controllers;

/// <summary>
/// Контроллер для работы с инвентарём (товарами).
/// Предоставляет методы получения информации о продуктах по идентификатору.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InventoryController(ILogger<InventoryController> logger, IInventoryService inventoryService) : ControllerBase
{
    /// <summary>
    /// Получает информацию о продукте из инвентаря по указанному ID.
    /// </summary>
    /// <param name="id"> Идентификатор продукта (целое неотрицательное число).</param>
    /// <param name="ct"> Токен отмены операции.</param>
    /// <returns> Объект продукта с кодом 200 OK или ошибку 400 Bad Request, если ID не указан или неверен.</returns>
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