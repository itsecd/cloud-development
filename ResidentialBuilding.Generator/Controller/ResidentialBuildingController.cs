using Generator.DTO;
using Generator.Service;
using Microsoft.AspNetCore.Mvc;

namespace Generator.Controller;

/// <summary>
///     Контроллер для объектов жилого строительства.
/// </summary>
[Route("api/residential-building")]
[ApiController]
public class ResidentialBuildingController(
    ILogger<ResidentialBuildingController> logger,
    IResidentialBuildingService residentialBuildingService) : ControllerBase
{
    /// <summary>
    ///     Получение объекта жилого строительства по id.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ResidentialBuildingDto>> GetResidentialBuilding(int id)
    {
        if (id <= 0)
        {
            return BadRequest("id must be >= 0");
        }

        logger.LogInformation("Getting residential building with Id={id}.", id);
        ResidentialBuildingDto result = await residentialBuildingService.GetByIdAsync(id);
        logger.LogInformation("Residential building with Id={id} successfully received.", id);

        return Ok(result);
    }
}