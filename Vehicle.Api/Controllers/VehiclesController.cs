using Microsoft.AspNetCore.Mvc;
using Vehicle.Api.Entities;
using Vehicle.Api.Services;

namespace Vehicle.Api.Controllers;

/// <summary>
/// Контроллер для управления транспортными средствами
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class VehiclesController(VehicleService vehicleService) : ControllerBase
{
    /// <summary>
    /// Возвращает транспортное средство по его идентификатору
    /// </summary>
    /// <param name="id">Идентификатор транспортного средства (должен быть больше 0)</param>
    /// <param name="cancellationToken">Токен для отмены запроса</param>
    /// <returns>Информация о найденном транспортном средстве</returns>
    [HttpGet]
    [ProducesResponseType(typeof(VehicleEntity), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetById([FromQuery] int? id, CancellationToken cancellationToken = default)
    {
        if (id is null || id <= 0)
        {
            return BadRequest("id must be > 0");
        }

        var vehicle = await vehicleService.GetByIdAsync(id.Value, cancellationToken);
        return Ok(vehicle);
    }

}