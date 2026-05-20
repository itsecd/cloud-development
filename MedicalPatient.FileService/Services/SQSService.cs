using Amazon.SQS;
using Amazon.SQS.Model;

namespace MedicalPatient.FileService.Services;

public class SQSService(
    IAmazonSQS sqsClient,
    ILogger<SQSService> logger,
    IConfiguration configuration) : IHostedService
{
    private readonly string _queueName = configuration["SQS:QueueName"] ?? "medical-patients";
    private readonly string _sqsUrl = configuration["SQS:ServiceUrl"] ?? "http://localhost:4566";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddMinutes(2);

        while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var createQueueRequest = new CreateQueueRequest
                {
                    QueueName = _queueName,
                    Attributes = new Dictionary<string, string>
                    {
                        { "VisibilityTimeout", "30" }
                    }
                };

                var response = await sqsClient.CreateQueueAsync(createQueueRequest, cancellationToken);
                logger.LogInformation("SQS queue '{QueueName}' is ready at {Url}", _queueName, response.QueueUrl);
                return;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "SQS not ready at {Url}, retrying in 2 seconds...", _sqsUrl);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }

        throw new TimeoutException($"SQS did not become ready within the timeout: {_sqsUrl}");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
