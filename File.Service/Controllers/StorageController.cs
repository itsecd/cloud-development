using System.Text.Json.Nodes;
using File.Service.Storage;
using Microsoft.AspNetCore.Mvc;

namespace File.Service.Controllers;

/// <summary>
/// Контроллер для просмотра содержимого объектного хранилища
/// </summary>
/// <param name="storage">Файловое хранилище</param>
/// <param name="logger">Логгер</param>
[ApiController]
[Route("api/s3")]
public sealed class StorageController(IFileStorage storage, ILogger<StorageController> logger) : ControllerBase
{
    /// <summary>
    /// Получить список всех ключей в бакете
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<string>>> ListFiles()
    {
        logger.LogInformation("Listing files in storage");
        var list = await storage.ListAsync();
        return Ok(list);
    }

    /// <summary>
    /// Скачать содержимое файла по ключу
    /// </summary>
    /// <param name="key">Ключ файла</param>
    [HttpGet("{key}")]
    public async Task<ActionResult<JsonNode>> GetFile(string key)
    {
        logger.LogInformation("Downloading file {key}", key);
        var node = await storage.DownloadAsync(key);
        return Ok(node);
    }
}
