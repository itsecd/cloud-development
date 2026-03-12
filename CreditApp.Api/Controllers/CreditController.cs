using CreditApp.Api.Services;
using CreditApp.Domain.Data;
using Microsoft.AspNetCore.Mvc;

namespace CreditApp.Api.Controllers;

/// <summary>
/// Контроллер для работы с кредитными заявками
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CreditController(
    ICreditService creditService,
    ILogger<CreditController> logger)
    : ControllerBase
{
    /// <summary>
    /// Получить кредитную заявку по идентификатору
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CreditApplication), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreditApplication>> Get(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
            return BadRequest("Id must be positive number");

        logger.LogInformation("Request credit application {CreditId}", id);

        var result = await creditService.GetAsync(id, cancellationToken);

        return Ok(result);
    }
}