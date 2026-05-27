using Microsoft.AspNetCore.Mvc;
using ProjectApp.Api.Messaging;
using ProjectApp.Api.Services.CreditApplicationGeneratorService;
using ProjectApp.Domain.Entities;

namespace ProjectApp.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CreditApplicationController(
    ICreditApplicationGeneratorService generatorService,
    CreditApplicationGeneratedEventProducer eventProducer,
    ILogger<CreditApplicationController> logger) : ControllerBase
{
    /// <summary>
    /// Получить кредитную заявку по ID, если не найдена в кэше — сгенерировать новую
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<CreditApplication>> GetById([FromQuery] int id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received request to retrieve/generate credit application {Id}", id);
        var application = await generatorService.GetByIdAsync(id, cancellationToken);
        await eventProducer.ProduceAsync(application, cancellationToken);

        return Ok(application);
    }
}
