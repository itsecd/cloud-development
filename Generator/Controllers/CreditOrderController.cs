using Generator.Dto;
using Generator.Services;
using Microsoft.AspNetCore.Mvc;

namespace Generator.Controllers;

/// <summary>
/// HTTP API для получения кредитной заявки по идентификатору.
/// Использует <see cref="Services.CreditOrderService"/> для получения данных (кэш + генерация).
/// </summary>
[ApiController]
[Route("credit-orders")]
public class CreditOrderController (
    CreditOrderService service,
    ILogger<CreditOrderController> logger
    ) : ControllerBase
{

    /// <summary>
    /// Возвращает кредитную заявку по <paramref name="id"/> из query string.
    /// </summary>
    /// <param name="id">Идентификатор заявки (должен быть больше 0).</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <returns>DTO кредитной заявки.</returns>
    /// <response code="200">Заявка успешно получена.</response>
    /// <response code="400">Некорректный идентификатор (id &lt;= 0).</response>
    [HttpGet]
    public async Task<ActionResult<CreditOrderDto>> Get([FromQuery] int id, CancellationToken ct)
    {
        logger.LogInformation("HTTP GET /credit-orders requested: {OrderId}", id);
        if (id <= 0)
            return BadRequest("id must be greater than 0");
        var order = await service.GetByIdAsync(id, ct);
        logger.LogInformation("HTTP GET /credit-orders completed: {OrderId} {Status}", order.Id, order.OrderStatus);
        return Ok(order);
    }
}
