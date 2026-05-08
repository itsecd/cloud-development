using Cloud.EventSink.S3;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;

namespace Cloud.EventSink.Controller;

/// <summary>
/// Контроллер для получения списка файлов и скачивания файлов из объектного хранилища
/// </summary>
/// <param name="s3Service">Сервис для работы с S3 хранилищем</param>
/// <param name="logger">Логгер</param>
[ApiController]
[Route("api/s3")]
public class S3StorageController(
    IS3Service s3Service, 
    ILogger<S3StorageController> logger
    ) : ControllerBase
{
    /// <summary>
    /// Метод получения списка названий всех файлов в S3 хранилище
    /// </summary>
    /// <response code="200">Успешное получение списка названий всех файлов</response>
    /// <response code="500">Ошибка чтения файлов в объектном хранилище</response>
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    [HttpGet]
    public async Task<ActionResult<List<string>>> ListFiles()
    {
        try
        {
            var files = await s3Service.GetFileList();
            return Ok(files);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing files");
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Метод получения содержимого файла по его наазванию
    /// </summary>
    /// <param name="key">Название файла в хранилище</param>
    /// <returns>Строковое представление файла</returns>
    /// <response code="200">Успешное получение файла</response>
    /// <response code="404">Файл не найден в объектном хранилище</response>
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    [HttpGet("{key}")]
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
            return NotFound(ex.Message);
        }
    }
}