using Microsoft.AspNetCore.Mvc;
using Service.FileStorage.Storage;
using System.Text.Json.Nodes;

namespace Service.FileStorage.Controllers;

/// <summary>
/// Контроллер для взаимодействия с объектным хранилищем S3
/// </summary>
/// <param name="s3Service">Служба для работы с S3</param>
/// <param name="logger">Логгер</param>
[ApiController]
[Route("api/s3")]
public class S3StorageController(IS3Service s3Service, ILogger<S3StorageController> logger) : ControllerBase
{
    /// <summary>
    /// Получает список хранящихся в S3 файлов
    /// </summary>
    /// <returns>Список с ключами файлов</returns>
    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<string>>> ListFiles()
    {
        try
        {
            var list = await s3Service.GetFileList();
            logger.LogInformation("Listed {Count} files from bucket", list.Count);
            return Ok(list);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing files");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Получает строковое представление хранящегося в S3 документа
    /// </summary>
    /// <param name="key">Ключ файла</param>
    /// <returns>JSON-представление файла</returns>
    [HttpGet("{key}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<JsonNode>> GetFile(string key)
    {
        try
        {
            var node = await s3Service.DownloadFile(key);
            return Ok(node);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error downloading file {Key}", key);
            return BadRequest(ex.Message);
        }
    }
}
