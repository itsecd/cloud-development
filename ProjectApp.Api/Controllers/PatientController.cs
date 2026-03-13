using Microsoft.AspNetCore.Mvc;
using ProjectApp.Api.Services.ProjectGeneratorService;
using ProjectApp.Domain.Entities;

namespace ProjectApp.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PatientController(
    IMedicalPatientGeneratorService generatorService,
    ILogger<PatientController> logger) : ControllerBase
{
    /// <summary>
    /// Получить медицинского пациента по ID, если не найден в кэше, сгенерировать нового
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<MedicalPatient>> GetById([FromQuery] int id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received request to retrieve/generate patient {Id}", id);

        var patient = await generatorService.GetByIdAsync(id, cancellationToken);

        return Ok(patient);
    }
}
