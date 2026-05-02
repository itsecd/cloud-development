using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json.Nodes;
using Vehicle.EventSink.Storage;

namespace Vehicle.EventSink.Controllers;

/// <summary>
/// Контроллер для взаимодействия с S3-совместимым хранилищем Minio.
/// </summary>
/// <param name="s3Service">Служба для работы с S3.</param>
/// <param name="logger">Логгер.</param>
[Route("api/s3")]
[ApiController]
public class S3StorageController(IS3Service s3Service, ILogger<S3StorageController> logger) : ControllerBase
{
    /// <summary>
    /// Получает список файлов, сохраненных в Minio.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<string>>> ListFiles()
    {
        logger.LogInformation("Method {Method} of {Controller} was called", nameof(ListFiles), nameof(S3StorageController));
        try
        {
            var list = await s3Service.GetFileList();
            logger.LogInformation("Got a list of {Count} files from bucket", list.Count);
            return Ok(list);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred during {Method} of {Controller}", nameof(ListFiles), nameof(S3StorageController));
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Получает JSON-файл из Minio по ключу.
    /// </summary>
    /// <param name="key">Ключ файла.</param>
    [HttpGet("{key}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<JsonNode>> GetFile(string key)
    {
        logger.LogInformation("Method {Method} of {Controller} was called", nameof(GetFile), nameof(S3StorageController));

        try
        {
            var node = await s3Service.DownloadFile(key);

            logger.LogInformation("Received JSON of {Size} bytes", Encoding.UTF8.GetByteCount(node.ToJsonString()));
            return Ok(node);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred during {Method} of {Controller}", nameof(GetFile), nameof(S3StorageController));
            return BadRequest(ex.Message);
        }
    }
}
