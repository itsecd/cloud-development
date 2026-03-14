using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Patient.Generator.DTO;
using Patient.Generator.Service;

namespace Patient.Generator.Controller;

/// <summary>
/// API контроллер для работы с медицинскими пациентами.
/// </summary>
[ApiController]
[Route("api/patient")]
public sealed class PatientController(ILogger<PatientController> logger, IPatientService service) : ControllerBase
{
    /// <summary>
    /// Получить пациента по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор пациента.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Данные пациента.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PatientDto>> Get([FromQuery] int id, CancellationToken cancellationToken)
    {
        if (id < 0)
        {
            return BadRequest(new { message = "id cannot be negative" });
        }

        logger.LogInformation("Request patient id={id}.", id);
        var dto = await service.GetAsync(id, cancellationToken);
        logger.LogInformation("Response patient id={id}.", id);

        return Ok(dto);
    }
}
