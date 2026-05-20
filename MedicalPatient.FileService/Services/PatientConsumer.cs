using Amazon.S3;
using Amazon.S3.Model;
using MassTransit;
using System.Text.Json;

namespace MedicalPatient.FileService.Services;

public class PatientConsumer(
    IAmazonS3 s3Client,
    ILogger<PatientConsumer> logger,
    string bucketName
) : IConsumer<MedicalPatientMessage>
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task Consume(ConsumeContext<MedicalPatientMessage> context)
    {
        try
        {
            var patient = context.Message;
            var fileName = $"patient-{patient.Id}.json";
            var json = JsonSerializer.Serialize(patient, _jsonOptions);

            await s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucketName,
                Key = fileName,
                ContentBody = json,
                ContentType = "application/json"
            }, context.CancellationToken);

            logger.LogInformation("Saved patient {PatientId} to S3 with filename: {FileName}", patient.Id, fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save patient data to S3");
        }
    }
}