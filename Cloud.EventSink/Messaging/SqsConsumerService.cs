using Amazon.SQS;
using Amazon.SQS.Model;
using Cloud.EventSink.S3;

namespace Cloud.EventSink.Messaging;

/// <summary>
/// Фоновая служба, читающая SQS сообщения и сохраняющая их в S3.
/// </summary>
public sealed class SqsConsumerService : BackgroundService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _queueUrl;
    private readonly ILogger<SqsConsumerService> _logger;

    public SqsConsumerService(
        IAmazonSQS sqsClient,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<SqsConsumerService> logger)
    {
        _sqsClient = sqsClient;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _queueUrl = configuration["AWS:Resources:SQSQueueUrl"]
                    ?? throw new KeyNotFoundException("SQS queue URL not found in configuration.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var startupScope = _scopeFactory.CreateScope())
        {
            var s3 = startupScope.ServiceProvider.GetRequiredService<IS3Service>();
            await s3.EnsureBucketExists();
        }

        _logger.LogInformation("SQS consumer started, polling queue: {QueueUrl}", _queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = _queueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 20
            }, stoppingToken);

            if (response?.Messages is null || response.Messages.Count == 0)
                continue;

            foreach (var message in response.Messages)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var s3 = scope.ServiceProvider.GetRequiredService<IS3Service>();
                    await s3.UploadFile(message.Body);
                    await _sqsClient.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, stoppingToken);
                    _logger.LogInformation("Processed message {MessageId}", message.MessageId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process message {MessageId}", message.MessageId);
                }
            }
        }
    }
}