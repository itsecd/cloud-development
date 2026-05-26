using System.Text;
using System.Text.Json;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using ProjectApp.Domain.Messaging;

namespace ProjectApp.FileService.ObjectStorage;

public sealed class MinioPatientFileStorage(
    IMinioClient minioClient,
    ObjectStorageOptions options,
    ILogger<MinioPatientFileStorage> logger) : IPatientFileStorage
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task SaveAsync(PatientGeneratedMessage message, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        var objectName = GetObjectName(message.Patient.Id);
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var putArgs = new PutObjectArgs()
            .WithBucket(options.BucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType("application/json");

        await minioClient.PutObjectAsync(putArgs, cancellationToken);

        logger.LogInformation(
            "Stored generated patient {Id} as object {ObjectName} in bucket {BucketName}",
            message.Patient.Id,
            objectName,
            options.BucketName);
    }

    public async Task<string?> GetPatientJsonAsync(int patientId, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        await using var stream = new MemoryStream();
        var getArgs = new GetObjectArgs()
            .WithBucket(options.BucketName)
            .WithObject(GetObjectName(patientId))
            .WithCallbackStream(source => source.CopyTo(stream));

        try
        {
            await minioClient.GetObjectAsync(getArgs, cancellationToken);
        }
        catch (ObjectNotFoundException)
        {
            return null;
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        var bucketExistsArgs = new BucketExistsArgs().WithBucket(options.BucketName);
        var exists = await minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);

        if (exists)
        {
            return;
        }

        var makeBucketArgs = new MakeBucketArgs().WithBucket(options.BucketName);
        await minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken);
    }

    private static string GetObjectName(int patientId) => $"patients/{patientId}.json";
}
