using File.Service.Storage;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;

namespace File.Service.Controllers;

/// <summary>
/// Контроллер для получения сохранённых в S3 файлов транспортных средств
/// </summary>
/// <param name="storage">Сервис S3-хранилища</param>
/// <param name="logger">Логгер</param>
[ApiController]
[Route("api/files")]
public class VehicleFilesController(
    IVehicleStorageService storage,
    ILogger<VehicleFilesController> logger) : ControllerBase
{
    /// <summary>
    /// Возвращает список ключей файлов в бакете
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<string>>> List()
    {
        var keys = await storage.ListKeys();
        return Ok(keys);
    }

    /// <summary>
    /// Возвращает JSON-файл транспортного средства по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор транспортного средства</param>
    /// <returns>JSON транспортного средства; 404, если файл отсутствует</returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<JsonNode>> Get(int id)
    {
        var node = await storage.Download(IVehicleStorageService.KeyFor(id));
        if (node is null)
        {
            logger.LogInformation("Vehicle {Id} not found in storage", id);
            return NotFound();
        }
        return Ok(node);
    }
}
