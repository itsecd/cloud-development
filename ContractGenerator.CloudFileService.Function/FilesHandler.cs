using Amazon.S3;
using Amazon.S3.Model;
using ContractGenerator.Shared.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ContractGenerator.CloudFileService.Function;

/// <summary>
/// HTTP-обработчик просмотра файлов сотрудников в Object Storage.
/// </summary>
public class FilesHandler
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<FilesHandler> _logger;
    private readonly string _bucketName;

    public FilesHandler()
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        var provider = services.BuildServiceProvider();

        _logger = provider.GetRequiredService<ILogger<FilesHandler>>();
        _s3Client = YandexStorageFactory.CreateClient(configuration);
        _bucketName = YandexStorageFactory.GetBucketName(configuration);
    }

    /// <summary>
    /// Обрабатывает HTTP-запросы /api/files и /api/files/{id}.
    /// </summary>
    public async Task<FunctionResponse> FunctionHandler(FunctionRequest request)
    {
        if (string.Equals(request.HttpMethod, "OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            return JsonResponse(HttpStatusCode.NoContent, string.Empty);
        }

        if (TryReadId(request, out var id))
        {
            return await GetEmployeeFileAsync(id);
        }

        return await ListEmployeeFilesAsync();
    }

    private async Task<FunctionResponse> ListEmployeeFilesAsync()
    {
        var keys = new List<string>();
        var paginator = _s3Client.Paginators.ListObjectsV2(new ListObjectsV2Request
        {
            BucketName = _bucketName
        });

        await foreach (var response in paginator.Responses)
        {
            if (response.S3Objects is null)
            {
                continue;
            }

            keys.AddRange(response.S3Objects.Where(s3Object => s3Object.Key is not null).Select(s3Object => s3Object.Key));
        }

        return JsonResponse(HttpStatusCode.OK, JsonSerializer.Serialize(keys, _jsonOptions));
    }

    private async Task<FunctionResponse> GetEmployeeFileAsync(int id)
    {
        var key = EmployeeFileKeys.ForId(id);

        try
        {
            using var response = await _s3Client.GetObjectAsync(_bucketName, key);
            using var reader = new StreamReader(response.ResponseStream, Encoding.UTF8);
            return JsonResponse(HttpStatusCode.OK, await reader.ReadToEndAsync());
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Object Storage file {Key} was not found", key);
            return JsonResponse(HttpStatusCode.NotFound, JsonSerializer.Serialize(new { error = "Employee file was not found" }, _jsonOptions));
        }
    }

    private static bool TryReadId(FunctionRequest request, out int id)
    {
        id = 0;

        if (request.PathParameters is not null
            && request.PathParameters.TryGetValue("id", out var pathId)
            && int.TryParse(pathId, out id)
            && id > 0)
        {
            return true;
        }

        var path = request.Path;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var lastSegment = path.TrimEnd('/').Split('/').LastOrDefault();
        return int.TryParse(lastSegment, out id) && id > 0;
    }

    private static FunctionResponse JsonResponse(HttpStatusCode statusCode, string body) => new()
    {
        StatusCode = (int)statusCode,
        Headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json",
            ["Access-Control-Allow-Origin"] = "*",
            ["Access-Control-Allow-Methods"] = "GET,OPTIONS",
            ["Access-Control-Allow-Headers"] = "Content-Type,Authorization"
        },
        Body = body
    };
}
