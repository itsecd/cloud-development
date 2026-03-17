using CreditApp.Api.Services.CreditApplicationService;
using CreditApp.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CreditApp.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CreditController(CreditApplicationService applicationService, ILogger<CreditController> logger) : ControllerBase
{
    /// <summary>
    /// Получить кредитную заявку по ID, если не найдена в кэше генерируем новую
    /// </summary>
    /// <param name="id">ID кредитной заявки</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Кредитная заявка</returns>
    [HttpGet]
    public async Task<ActionResult<CreditApplication>> GetById([FromQuery] int id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Получен запрос на получение/генерацию заявки {Id}", id);

        var application = await applicationService.GetByIdAsync(id, cancellationToken);

        return Ok(application);
    }
}
