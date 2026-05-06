using System.Text.Json.Nodes;
using CourseApp.FileService.Storage;
using Microsoft.AspNetCore.Mvc;

namespace CourseApp.FileService.Controllers;

/// <summary>
/// HTTP-фасад над S3 для интеграционных тестов и ручной отладки
/// </summary>
/// <param name="s3Service">Служба для работы с S3</param>
/// <param name="logger">Логгер</param>
[ApiController]
[Route("api/s3")]
public sealed class S3StorageController(IS3Service s3Service, ILogger<S3StorageController> logger) : ControllerBase
{
    /// <summary>
    /// Возвращает список всех ключей в бакете
    /// </summary>
    /// <returns>200 с массивом ключей; 500 при ошибке</returns>
    [HttpGet]
    public async Task<ActionResult<List<string>>> ListFiles()
    {
        try
        {
            return Ok(await s3Service.GetFileList());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list bucket contents");
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Возвращает JSON-документ по ключу объекта
    /// </summary>
    /// <param name="key">Ключ объекта в S3 (например, course_42.json)</param>
    /// <returns>200 с телом JSON; 500 при ошибке</returns>
    [HttpGet("{key}")]
    public async Task<ActionResult<JsonNode>> GetFile(string key)
    {
        try
        {
            return Ok(await s3Service.DownloadFile(key));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download {Key}", key);
            return StatusCode(500, ex.Message);
        }
    }
}
