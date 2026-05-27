using ContractGenerator.FileService.Storage;
using ContractGenerator.Shared.Storage;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;

namespace ContractGenerator.FileService.Controllers;

/// <summary>
/// Контроллер для просмотра сохраненных JSON-файлов сотрудников.
/// </summary>
/// <param name="storage">S3-хранилище файлов сотрудников.</param>
/// <param name="logger">Логгер.</param>
[ApiController]
[Route("api/files")]
public class EmployeeFilesController(
    IEmployeeFileStorage storage,
    ILogger<EmployeeFilesController> logger) : ControllerBase
{
    /// <summary>
    /// Возвращает список ключей файлов в S3.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> List(CancellationToken cancellationToken)
    {
        var keys = await storage.ListKeysAsync(cancellationToken);
        return Ok(keys);
    }

    /// <summary>
    /// Возвращает сохраненный JSON сотрудника по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор сотрудника.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(JsonNode), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JsonNode>> Get(int id, CancellationToken cancellationToken)
    {
        var key = EmployeeFileKeys.ForId(id);
        var employee = await storage.DownloadAsync(key, cancellationToken);
        if (employee is null)
        {
            logger.LogInformation("Employee file {Key} was not found", key);
            return NotFound();
        }

        return Ok(employee);
    }
}
