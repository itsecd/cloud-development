using Microsoft.AspNetCore.Mvc;
using CreditSystem.Api.Services;
using CreditSystem.Domain.Entities;

namespace CreditSystem.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class LoansController(LoanService loanService, ILogger<LoansController> logger) : ControllerBase
{
    /// <summary>
    /// Получить данные по кредитной заявке (с генерацией при отсутствии)
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LoanApplication>> Get(Guid id, CancellationToken ct)
    {
        logger.LogInformation("Запрос на получение данных по заявке: {Id}", id);
        var result = await loanService.GetApplicationAsync(id, ct);
        return Ok(result);
    }
}
