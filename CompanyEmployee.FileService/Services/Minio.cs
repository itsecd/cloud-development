using Minio;
using Minio.DataModel.Args;
using System.Text;
namespace CompanyEmployee.FileService.Services;

public class MinioService
{
    private readonly IMinioClient _client;

    private readonly string _bucket;

    public MinioService(IConfiguration configuration)
    {
        _bucket = configuration["Minio:BucketName"]!;

        var endpoint = configuration["Minio:Endpoint"]!;
        var parts = endpoint.Split(':');

        _client = new MinioClient()
            .WithEndpoint(parts[0], int.Parse(parts[1]))
            .WithCredentials(configuration["Minio:AccessKey"], configuration["Minio:SecretKey"])
            .Build();
    }

    public async Task InitializeAsync()
    {
        var exists = await _client.BucketExistsAsync( new BucketExistsArgs().WithBucket(_bucket));
        if (!exists)
        {
            await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucket));
        }
    }

    public async Task UploadJsonAsync(string fileName, string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        using var stream = new MemoryStream(bytes);

        await _client.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_bucket)
                .WithObject(fileName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType("application/json"));
    }
}