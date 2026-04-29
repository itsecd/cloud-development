using File.Service.Storage;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;

namespace File.Service.Controllers;

/// <summary>
/// HTTP-контроллер для просмотра содержимого S3-бакета
/// </summary>
/// <param name="storage">Служба объектного хранилища</param>
/// <param name="logger">Структурный логгер</param>
[ApiController]
[Route("api/s3")]
public class StorageController(IObjectStorage storage, ILogger<StorageController> logger) : ControllerBase
{
    /// <summary>
    /// Возвращает список ключей всех файлов, лежащих в бакете
    /// </summary>
    /// <returns>Коллекция строковых ключей объектов в бакете</returns>
    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<string>>> List()
    {
        logger.LogInformation("Listing files in object storage");
        try
        {
            var keys = await storage.ListProjects();
            logger.LogInformation("Got {Count} keys from bucket", keys.Count);
            return Ok(keys);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list files");
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Возвращает JSON-содержимое файла из бакета по его ключу
    /// </summary>
    /// <param name="key">Ключ файла внутри бакета (например, <c>software-project-42.json</c>)</param>
    /// <returns>Десериализованный JSON-документ</returns>
    [HttpGet("{key}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<JsonNode>> Get(string key)
    {
        logger.LogInformation("Downloading file {Key} from object storage", key);
        try
        {
            var node = await storage.DownloadProject(key);
            return Ok(node);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download file {Key}", key);
            return StatusCode(500, ex.Message);
        }
    }
}
