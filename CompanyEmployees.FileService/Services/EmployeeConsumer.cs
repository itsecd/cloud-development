using Amazon.S3;
using Amazon.S3.Model;
using MassTransit;
using System.Text.Json;
using CompanyEmployees.FileService.Models;

namespace CompanyEmployees.FileService.Services;

public class EmployeeConsumer(
    IAmazonS3 s3Client,
    ILogger<EmployeeConsumer> logger,
    IConfiguration configuration
) : IConsumer<EmployeeMessage>
{
    private readonly string _bucketName = configuration["MinIO:BucketName"] ?? "company-employee";
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task Consume(ConsumeContext<EmployeeMessage> context)
    { 
        var employee= context.Message;
        var fileName = $"employee-{employee.Id}.json";
        var json = JsonSerializer.Serialize(employee, _jsonOptions);

        await s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = fileName,
            ContentBody = json,
            ContentType = "application/json"
        }, context.CancellationToken);

        logger.LogInformation("Saved employee {EmployeeId} to minio with filename: {FileName}", employee.Id, fileName);
    }
}