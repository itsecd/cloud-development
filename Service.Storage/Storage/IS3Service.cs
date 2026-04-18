using System.Text.Json.Nodes;

namespace Service.Storage.Storage;

public interface IS3Service
{
    /// <summary>
    /// sends file in storage
    /// </summary>
    /// <param name="fileData">saving file string representation</param>
    public Task<bool> UploadFile(string fileData);

    /// <summary>
    /// getting all files list from the storage
    /// </summary>
    /// <returns>Paths list</returns>
    public Task<List<string>> GetFileList();

    /// <summary>
    /// Getting string representation of a file from the storage
    /// </summary>
    /// <param name="filePath">Bucket file path</param>
    /// <returns>Read file string representation</returns>
    public Task<JsonNode> DownloadFile(string filePath);

    /// <summary>
    /// Creates s3 bucket if needed
    /// </summary>
    public Task EnsureBucketExists();
}
