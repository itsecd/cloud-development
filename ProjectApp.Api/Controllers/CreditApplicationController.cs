using ProjectApp.Api.Services.CreditApplicationService;
using ProjectApp.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ProjectApp.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CreditApplicationController(ICreditApplicationService creditService, ILogger<CreditApplicationController> logger) : ControllerBase
{
    /// <summary>
    /// Получить кредитную заявку по ID, если не найдена в кэше — сгенерировать новую
    /// </summary>
    /// <param name="id">ID кредитной заявки</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Кредитная заявка</returns>
    [HttpGet]
    public async Task<ActionResult<CreditApplication>> GetById([FromQuery] int id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received request to retrieve/generate credit application {Id}", id);

        var application = await creditService.GetByIdAsync(id, cancellationToken);

        return Ok(application);
    }
}
