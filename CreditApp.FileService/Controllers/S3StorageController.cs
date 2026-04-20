using CreditApp.FileService.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;

namespace CreditApp.FileService.Controllers;

/// <summary>
/// Контроллер для взаимодействия с S3 хранилищем
/// </summary>
[ApiController]
[Route("api/s3")]
public class S3StorageController(IFileStorage storage, ILogger<S3StorageController> logger) : ControllerBase
{
    /// <summary>
    /// Возвращает список ключей файлов, хранящихся в бакете
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<string>>> ListFiles(CancellationToken cancellationToken)
    {
        logger.LogInformation("Listing files in S3 bucket");
        var list = await storage.GetFileListAsync(cancellationToken);
        return Ok(list);
    }

    /// <summary>
    /// Возвращает содержимое файла из бакета по ключу
    /// </summary>
    [HttpGet("{key}")]
    public async Task<ActionResult<JsonNode>> GetFile(string key, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching file {key} from S3 bucket", key);
        var node = await storage.DownloadAsync(key, cancellationToken);
        return Ok(node);
    }
}
