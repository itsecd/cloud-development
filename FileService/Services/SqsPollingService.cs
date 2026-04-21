using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;

namespace FileService.Services;

public sealed class SqsPollingService(
    ILogger<SqsPollingService> logger,
    MinioStorageService storage,
    IConfiguration configuration) : BackgroundService
{
    private readonly string _topicName = configuration["Sns:TopicName"] ?? "medical-patients";
    private readonly string _queueName = configuration["Sqs:QueueName"] ?? "medical-patients-queue";
    private readonly string _awsServiceUrl = configuration["AWS:ServiceURL"] ?? "http://localhost:4566";

    private string? _queueUrl;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var credentials = new BasicAWSCredentials("test", "test");
        using var sqs = new AmazonSQSClient(credentials, new AmazonSQSConfig { ServiceURL = _awsServiceUrl });
        using var sns = new AmazonSimpleNotificationServiceClient(credentials,
            new AmazonSimpleNotificationServiceConfig { ServiceURL = _awsServiceUrl });

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SetupAsync(sns, sqs, stoppingToken);
                break;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Infrastructure setup failed, retrying in 5s");
                await Task.Delay(5000, stoppingToken);
            }
        }

        logger.LogInformation("SQS polling started. Queue={QueueUrl}", _queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAsync(sqs, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Polling error");
                await Task.Delay(2000, stoppingToken);
            }
        }
    }

    private async Task SetupAsync(
        IAmazonSimpleNotificationService sns,
        IAmazonSQS sqs,
        CancellationToken ct)
    {
        var topicArn = (await sns.CreateTopicAsync(_topicName, ct)).TopicArn;

        _queueUrl = (await sqs.CreateQueueAsync(_queueName, ct)).QueueUrl;

        var attrs = await sqs.GetQueueAttributesAsync(_queueUrl, ["QueueArn"], ct);
        var queueArn = attrs.QueueARN;

        var subscriptions = await sns.ListSubscriptionsByTopicAsync(topicArn, ct);
        if (!subscriptions.Subscriptions.Any(s => s.Endpoint == queueArn))
        {
            await sns.SubscribeAsync(topicArn, "sqs", queueArn, ct);
            await sqs.SetQueueAttributesAsync(_queueUrl, new Dictionary<string, string>
            {
                ["Policy"] = $$$"""
                    {
                        "Version": "2012-10-17",
                        "Statement": [{
                            "Effect": "Allow",
                            "Principal": "*",
                            "Action": "sqs:SendMessage",
                            "Resource": "{{{queueArn}}}",
                            "Condition": {"ArnEquals": {"aws:SourceArn": "{{{topicArn}}}"}}
                        }]
                    }
                    """
            }, ct);
        }
    }

    private async Task PollAsync(IAmazonSQS sqs, CancellationToken ct)
    {
        if (_queueUrl is null) return;

        var response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = _queueUrl,
            MaxNumberOfMessages = 10,
            WaitTimeSeconds = 5
        }, ct);

        foreach (var message in response.Messages)
        {
            try
            {
                using var envelope = JsonDocument.Parse(message.Body);
                var patientJson = envelope.RootElement.TryGetProperty("Message", out var msg)
                    ? msg.GetString() ?? message.Body
                    : message.Body;

                using var patientDoc = JsonDocument.Parse(patientJson);
                var patientId = patientDoc.RootElement.GetProperty("Id").GetInt32();

                await storage.SavePatientAsync(patientJson, patientId, ct);
                await sqs.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, ct);

                logger.LogInformation("Saved patient {PatientId} to MinIO", patientId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process SQS message");
            }
        }
    }
}
