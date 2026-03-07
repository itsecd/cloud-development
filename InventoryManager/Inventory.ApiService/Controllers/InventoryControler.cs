using Microsoft.AspNetCore.Mvc;
using Inventory.ApiService.Entity;
using Inventory.ApiService.Cache;

namespace Inventory.ApiService.Controllers;

/// <summary>
/// Контроллер для обработки запросов, связанных с продуктами
/// </summary>
/// <param name="cache"> Сервис кэширования продуктов</param>
[ApiController]
[Route("api/[controller]")]
public class InventoryController(IInventoryCache cache) : ControllerBase
{
    /// <summary>
    /// Обрабатывает GET-запрос на получение продукта по идентификатору
    /// </summary>
    /// <param name="id"> Идентификатор продукта</param>
    /// <param name="ct"> Токен отмены операции</param>
    /// <returns> Объект продукта или ошибка 400 при некорректном идентификаторе</returns>
    [HttpGet]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Product>> Get([FromQuery] int? id, CancellationToken ct)
    {
        if (id is null || id < 0)
            return BadRequest("id is required and must be >= 0");

        var product = await cache.GetAsync(id.Value, ct);
        return Ok(product);
    }
}