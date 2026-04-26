using AspireApp.FileService.Storage;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json.Nodes;

namespace AspireApp.FileService.Controllers;

/// <summary>
/// REST API контроллер для работы с объектным хранилищем S3
/// </summary>
/// <param name="s3Service">Служба для работы с S3</param>
/// <param name="logger">Логгер</param>
[ApiController]
[Route("api/s3")]
public class StorageController(IS3Service s3Service, ILogger<StorageController> logger) : ControllerBase
{
    /// <summary>
    /// Возвращает список ключей всех файлов в бакете
    /// </summary>
    /// <returns>200 со списком ключей или 500 при ошибке</returns>
    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<string>>> ListFiles()
    {
        try
        {
            var list = await s3Service.GetFileList();
            logger.LogInformation("Получен список из {Count} файлов", list.Count);
            return Ok(list);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка файлов");
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Возвращает содержимое файла по ключу
    /// </summary>
    /// <param name="key">Ключ объекта в бакете</param>
    /// <returns>200 c JSON-содержимым файла или 500 при ошибке</returns>
    [HttpGet("{key}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<JsonNode>> GetFile(string key)
    {
        try
        {
            var node = await s3Service.DownloadFile(key);
            logger.LogInformation("Файл {Key} получен, размер {Size} байт", key, Encoding.UTF8.GetByteCount(node.ToJsonString()));
            return Ok(node);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при скачивании файла {Key}", key);
            return StatusCode(500, ex.Message);
        }
    }
}
