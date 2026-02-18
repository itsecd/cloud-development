using CreditApp.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CreditApp.Api.Controllers;

/// <summary>
/// Контроллер для работы с кредитными заявками через HTTP API.
/// Реализует конечную точку получения заявки по идентификатору.
/// <param name="creditService">Сервис для получения данных кредитных заявок.</param>
/// <param name="logger">Логгер для записи событий и ошибок.</param>
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class CreditController(
    ICreditService creditService,
    ILogger<CreditController> logger
    ) : ControllerBase
{
    /// <summary>
    /// Получает кредитную заявку по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор запрашиваемой заявки (передаётся в строке запроса).</param>
    /// <param name="ct">Токен отмены для асинхронной операции.</param>
    /// <returns>HTTP 200 с объектом заявки при успешном получении.</returns>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int id, CancellationToken ct)
    {
        logger.LogInformation("Request for credit {CreditId} started", id);

        var result = await creditService.GetAsync(id, ct);

        logger.LogInformation("Request for credit {CreditId} completed", id);

        return Ok(result);
    }
}
