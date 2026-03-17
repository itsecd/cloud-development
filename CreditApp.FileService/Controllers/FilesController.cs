using CreditApp.FileService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CreditApp.FileService.Controllers;

/// <summary>
/// Контроллер для работы с файлами кредитных заявок в MinIO
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class FilesController(MinioStorageService minioStorage, ILogger<FilesController> logger) : ControllerBase
{
    /// <summary>
    /// Получить список всех файлов кредитных заявок из хранилища
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Список имён файлов в хранилище</returns>
    /// <response code="200">Список файлов успешно получен</response>
    /// <response code="500">Внутренняя ошибка сервера</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<string>>> GetFilesList(CancellationToken cancellationToken)
    {
        try
        {
            await minioStorage.EnsureBucketExistsAsync(cancellationToken);
            var files = await minioStorage.ListFilesAsync(cancellationToken);
            return Ok(files);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка файлов");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Получить содержимое файла кредитной заявки по имени
    /// </summary>
    /// <param name="fileName">Имя файла в хранилище</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>JSON содержимое файла кредитной заявки</returns>
    /// <response code="200">Файл успешно получен</response>
    /// <response code="404">Файл не найден</response>
    /// <response code="500">Внутренняя ошибка сервера</response>
    [HttpGet("{fileName}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFile(string fileName, CancellationToken cancellationToken)
    {
        try
        {
            var content = await minioStorage.GetFileContentAsync(fileName, cancellationToken);

            if (content == null)
            {
                return NotFound(new { error = "File not found" });
            }

            return Content(content, "application/json");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении файла {FileName}", fileName);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
