using Generator.DTO;
using Generator.Service;
using Microsoft.AspNetCore.Mvc;

namespace Generator.Controller;

/// <summary>
/// Контроллер для объектов жилого строительства.
/// </summary>
[Route("api/residential-building")]
[ApiController]
public class ResidentialBuildingController(ILogger<ResidentialBuildingController> logger, IResidentialBuildingService residentialBuildingService) : ControllerBase
{
    /// <summary>
    /// Получение объекта жилого строительства по id.
    /// </summary>
    /// <param name="id">Идентификатор объекта жилого строительства.</param>
    /// <returns>DTO объекта жилого строительства.</returns>
    /// <response code="200">Успешное получение объекта.</response>
    /// <response code="400">Некорректный id.</response>
    [HttpGet]
    [ProducesResponseType<ResidentialBuildingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResidentialBuildingDto>> GetResidentialBuilding([FromQuery] int id)
    {
        if (id <= 0)
        {
            return BadRequest("id must be >= 0");
        }
        
        logger.LogInformation("Getting residential building with Id={id}.", id);
        var result = await residentialBuildingService.GetByIdAsync(id);
        logger.LogInformation("Residential building with Id={id} successfully received.", id);

        return Ok(result);
    }
}