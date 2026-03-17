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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CreditApplication>> Get(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            if (id <= 0)
            {
                logger.LogWarning("Invalid credit application ID: {CreditId}", id);
                return BadRequest("Id must be positive number");
            }

            logger.LogInformation("Requesting credit application {CreditId}", id);

            var result = await creditService.GetAsync(id, cancellationToken);

            if (result == null)
            {
                logger.LogWarning("Credit application {CreditId} not found", id);
                return NotFound($"Credit application with ID {id} not found");
            }

            logger.LogInformation("Successfully retrieved credit application {CreditId}", id);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Request for credit application {CreditId} was cancelled", id);
            return StatusCode(499, "Request cancelled by client");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving credit application {CreditId}: {ErrorMessage}", id, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
        }
    }
}