using Microsoft.AspNetCore.Mvc;
using ResidentialBuilding.EventSink.Service.Storage;
using System.Text;
using System.Text.Json.Nodes;

namespace ResidentialBuilding.EventSink.Controller;

/// <summary>
/// Контроллер для взаимодейсвия с S3
/// </summary>
/// <param name="fileService">Служба для работы с S3</param>
/// <param name="logger">Логгер</param>
[ApiController]
[Route("api/s3")]
public class S3StorageController(IFileService fileService, ILogger<S3StorageController> logger) : ControllerBase
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
        logger.LogInformation("Method {method} of {controller} was called.", nameof(ListFiles),
            nameof(S3StorageController));
        try
        {
            var list = await fileService.GetFilesList();

            logger.LogInformation("Got a list of {count} files from bucket.", list.Count);
            return Ok(list);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occured during {method} of {controller}.", nameof(ListFiles),
                nameof(S3StorageController));
            return StatusCode(500, new { message = ex.Message });
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
        logger.LogInformation("Method {method} of {controller} was called.", nameof(GetFile),
            nameof(S3StorageController));
        try
        {
            var node = await fileService.DownloadFile(key);
            logger.LogInformation("Received json of {size} bytes.", Encoding.UTF8.GetByteCount(node.ToJsonString()));
            return Ok(node);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occured during {method} of {controller}.", nameof(GetFile),
                nameof(S3StorageController));
            return StatusCode(500, new { message = ex.Message });
        }
    }
}