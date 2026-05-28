using File.Service.Storage;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace File.Service.Controllers;

/// <summary>
/// Контроллер для работы с файлами транспортных средств в S3
/// </summary>
[ApiController]
[Route("api/files")]
public class VehicleFilesController(
    IVehicleStorageService storage,
    ILogger<VehicleFilesController> logger) : ControllerBase
{

    /// <summary>
    /// Возвращает список всех файлов в хранилище
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<string>>> GetAllFiles()
    {
        var keys = await storage.GetAllFileKeysAsync();
        return Ok(keys);
    }

    /// <summary>
    /// Возвращает файл транспортного средства по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор транспортного средства</param>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<JsonDocument>> GetVehicleFile(int id)
    {
        var fileKey = IVehicleStorageService.BuildFileKey(id);
        var document = await storage.FetchVehicleFileAsync(fileKey);

        if (document == null)
        {
            logger.LogInformation("Vehicle {Id} file not found", id);
            return NotFound($"Vehicle {id} not found in storage");
        }

        return Ok(document);
    }
}