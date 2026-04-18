using System.Text.Json.Nodes;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Service.Storage.Storage;

namespace Service.Storage.Controllers;

/// <summary>
/// Controller to deal with S3
/// </summary>
/// <param name="s3Service">Service to deal witg S3</param>
/// <param name="logger">Logger</param>
[ApiController]
[Route("api/s3")]
public class S3StorageController(IS3Service s3Service, ILogger<S3StorageController> logger) : ControllerBase
{
    /// <summary>
    /// Getting files from s3
    /// </summary>
    /// <returns>List with files keys</returns>
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
    /// Gets files string
    /// </summary>
    /// <param name="key">Key of the file</param>
    /// <returns>File string representation</returns>
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
