using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Minio;
using Minio.DataModel.Args;

namespace Service.Storage.Storage;

/// <summary>
/// Cлужба для манипуляции файлами в объектном хранилище
/// </summary>
/// <param name="client">S3 клиент</param>
/// <param name="configuration">Конфигурация</param>
/// <param name="logger">Логер</param>
public class S3MinioService(IMinioClient client, IConfiguration configuration, ILogger<S3MinioService> logger)
{
    private readonly string _bucketName = configuration["AWS:Resources:MinioBucketName"]
        ?? throw new KeyNotFoundException("S3 bucket name was not found in configuration");

    ///<inheritdoc/>
    public async Task<List<string>> GetFileList()
    {
        var list = new List<string>();
        var request = new ListObjectsArgs()
            .WithBucket(_bucketName)
            .WithPrefix("")
            .WithRecursive(true);
        logger.LogInformation("Began listing files in {bucket}", _bucketName);
        var responseList = client.ListObjectsEnumAsync(request);

        if (responseList == null)
            logger.LogWarning("Received null response from {bucket}", _bucketName);

        await foreach (var response in responseList!)
            list.Add(response.Key);
        return list;
    }

    ///<inheritdoc/>
    public async Task<bool> UploadFile(string fileData)
    {
        var rootNode = JsonNode.Parse(fileData) ?? throw new ArgumentException("Passed string is not a valid JSON");
        var id = rootNode["Id"]?.GetValue<int>() ?? throw new ArgumentException("Passed JSON has invalid structure");

        var bytes = Encoding.UTF8.GetBytes(fileData);
        using var stream = new MemoryStream(bytes);
        stream.Seek(0, SeekOrigin.Begin);

        logger.LogInformation("Began uploading credit order {file} onto {bucket}", id, _bucketName);
        var request = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithStreamData(stream)
            .WithObjectSize(bytes.Length)
            .WithObject($"credit-order_{id}.json");

        var response = await client.PutObjectAsync(request);

        if (response.ResponseStatusCode != HttpStatusCode.OK)
        {
            logger.LogError("Failed to upload credit order {file}: {code}", id, response.ResponseStatusCode);
            return false;
        }
        logger.LogInformation("Finished uploading credit order {file} to {bucket}", id, _bucketName);
        return true;
    }

    ///<inheritdoc/>
    public async Task<JsonNode> DownloadFile(string key)
    {
        logger.LogInformation("Began downloading {file} from {bucket}", key, _bucketName);

        try
        {
            var memoryStream = new MemoryStream();

            var request = new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(key)
                .WithCallbackStream(async (stream, cancellationToken) =>
                {
                    await stream.CopyToAsync(memoryStream, cancellationToken);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                });

            var response = await client.GetObjectAsync(request);

            if (response == null)
            {
                logger.LogError("Failed to download {file}", key);
                throw new InvalidOperationException($"Error occurred downloading {key} -  object is null");
            }
            using var reader = new StreamReader(memoryStream, Encoding.UTF8);
            return JsonNode.Parse(reader.ReadToEnd()) ?? throw new InvalidOperationException($"Downloaded document is not a valid JSON");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred during {file} downloading ", key);
            throw;
        }
    }

    ///<inheritdoc/>
    public async Task EnsureBucketExists()
    {
        logger.LogInformation("Checking whether {bucket} exists", _bucketName);
        try
        {
            var request = new BucketExistsArgs()
                .WithBucket(_bucketName);

            var exists = await client.BucketExistsAsync(request);
            if (!exists)
            {

                logger.LogInformation("Creating {bucket}", _bucketName);
                var createRequest = new MakeBucketArgs()
                    .WithBucket(_bucketName);
                await client.MakeBucketAsync(createRequest);
                return;
            }
            logger.LogInformation("{bucket} exists", _bucketName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred during {bucket} check", _bucketName);
            throw;
        }
    }
}
