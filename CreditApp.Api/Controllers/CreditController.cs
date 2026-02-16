using CreditApp.Api.Services.CreditGeneratorService;
using Microsoft.AspNetCore.Mvc;

namespace CreditApp.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CreditController(ICreditApplicationGeneratorService _generatorService, ILogger<CreditController> _logger) : ControllerBase
{
    /// <summary>
    /// Получить кредитную заявку по ID, если не найдена в кэше генерируем новую
    /// </summary>
    /// <param name="id">ID кредитной заявки</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Кредитная заявка</returns>
    [HttpGet]
    public async Task<IActionResult> GetById([FromQuery] int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Получен запрос на получение/генерацию заявки {Id}", id);
        
        var application = await _generatorService.GetByIdAsync(id, cancellationToken);
        
        return Ok(application);
    }
}
