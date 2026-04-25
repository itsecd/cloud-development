using System.Text.Json.Nodes;
using File.Service.Storage;
using Microsoft.AspNetCore.Mvc;

namespace File.Service.Controllers;

/// <summary>
/// HTTP-интерфейс на чтение содержимого бакета Minio.
/// Используется интеграционными тестами и для ручной проверки состояния хранилища
/// </summary>
/// <param name="s3Service">Сервис доступа к бакету</param>
/// <param name="logger">Логгер HTTP-обращений</param>
[ApiController]
[Route("api/s3")]
public class S3Controller(IS3Service s3Service, ILogger<S3Controller> logger) : ControllerBase
{
    /// <summary>
    /// Возвращает список ключей всех объектов в бакете
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<string>>> ListFiles()
    {
        logger.LogInformation("ListFiles was called");
        var list = await s3Service.GetFileList();
        return Ok(list);
    }

    /// <summary>
    /// Возвращает содержимое объекта по ключу. Если объекта нет — 404
    /// </summary>
    /// <param name="key">Ключ объекта в бакете, например <c>vehicle_42.json</c></param>
    [HttpGet("{key}")]
    public async Task<ActionResult<JsonNode>> GetFile(string key)
    {
        logger.LogInformation("GetFile was called for {key}", key);
        try
        {
            var node = await s3Service.DownloadFile(key);
            return Ok(node);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to download {key}", key);
            return NotFound();
        }
    }
}
