using Microsoft.AspNetCore.Mvc;
using Service.Storage.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Service.Storage.Controllers;

/// <summary>
/// Контроллер для взаимодейсвия с S3
/// </summary>
/// <param name="s3Service">Служба для работы с S3</param>
/// <param name="logger">Логгер</param>
[ApiController]
[Route("api/s3")]
public class S3StorageController(S3MinioService s3Service, ILogger<S3StorageController> logger) : ControllerBase
{
    /// <summary>
    /// Метод для получения списка хранящихся в S3 файлов
    /// </summary>
    /// <returns>Список с ключами файлов</returns>
    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<string>>> ListFiles()
    {
        logger.LogInformation("Method {method} of {controller} was called", nameof(ListFiles), nameof(S3StorageController));
        try
        {
            var list = await s3Service.GetFileList();
            logger.LogInformation("Got a list of {count} files from bucket", list.Count);
            return Ok(list);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occured during {method} of {controller}", nameof(ListFiles), nameof(S3StorageController));
            return BadRequest(ex);
        }
    }

    /// <summary>
    /// Получает строковое представление хранящегося в S3 документа
    /// </summary>
    /// <param name="key">Ключ файла</param>
    /// <returns>Строковое представление файла</returns>
    [HttpGet("{key}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<JsonNode>> GetFile(string key)
    {
        logger.LogInformation("Method {method} of {controller} was called", nameof(GetFile), nameof(S3StorageController));
        try
        {
            var node = await s3Service.DownloadFile(key);
            logger.LogInformation("Received json of {size} bytes", Encoding.UTF8.GetByteCount(node.ToJsonString()));
            return Ok(node);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occured during {method} of {controller}", nameof(GetFile), nameof(S3StorageController));
            return BadRequest(ex);
        }
    }
}
