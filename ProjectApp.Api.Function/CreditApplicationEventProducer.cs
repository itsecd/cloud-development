using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace ProjectApp.Api.Function;

public class CreditApplicationEventProducer
{
    public async Task ProduceGeneratedAsync(CreditApplication application)
    {
        var queueUrl = Environment.GetEnvironmentVariable("SQS_QUEUE_URL");
        var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

        if (string.IsNullOrWhiteSpace(queueUrl) ||
            string.IsNullOrWhiteSpace(accessKey) ||
            string.IsNullOrWhiteSpace(secretKey))
        {
            Console.WriteLine("[WARN] Message Queue settings are not configured");
            return;
        }

        using var sqs = CreateSqsClient(accessKey, secretKey);
        var evt = new CreditApplicationGeneratedEvent
        {
            Id = application.Id,
            OccurredAtUtc = DateTime.UtcNow,
            Application = application
        };

        await sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = JsonSerializerDefaultsProvider.Serialize(evt)
        });
    }

    private static IAmazonSQS CreateSqsClient(string accessKey, string secretKey)
    {
        var endpoint = Environment.GetEnvironmentVariable("SQS_ENDPOINT")
            ?? "https://message-queue.api.cloud.yandex.net";

        return new AmazonSQSClient(
            new BasicAWSCredentials(accessKey, secretKey),
            new AmazonSQSConfig
            {
                ServiceURL = endpoint,
                AuthenticationRegion = Environment.GetEnvironmentVariable("YC_REGION") ?? "ru-central1"
            });
    }
}
