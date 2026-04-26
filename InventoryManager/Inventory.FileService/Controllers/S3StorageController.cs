using Inventory.FileService.Storage;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json.Nodes;

namespace Inventory.FileService.Controllers;

/// <summary>
/// Контроллер для работы с файлами, хранящимися в S3-хранилище
/// </summary>
/// <param name="s3Service">Сервис для выполнения операций с S3-хранилищем</param>
/// <param name="logger">Сервис логирования работы контроллера</param>
[ApiController]
[Route("api/s3")]
public class S3StorageController(IS3Service s3Service, ILogger<S3StorageController> logger) : ControllerBase
{
    /// <summary>
    /// Получает список всех файлов из S3-хранилища
    /// </summary>
    /// <returns>Список ключей файлов, находящихся в S3-хранилище</returns>
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
    /// Получает содержимое JSON-файла из S3-хранилища по его ключу
    /// </summary>
    /// <param name="key">Ключ файла в S3-хранилище</param>
    /// <returns>Содержимое файла в формате JSON</returns>
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