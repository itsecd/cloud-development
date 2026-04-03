using Amazon.SQS;
using Amazon.SQS.Model;
using CreditApp.FileService;
using CreditApp.Messaging.Contracts;
using System.Text;
using System.Text.Json;

/// <summary>
/// Consumer для получения сообщений из очереди SQS и сохранения в S3
/// </summary>
public class SqsConsumer : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SqsConsumer> _logger;
    private readonly string _queueUrl = "http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/credit-queue";

    public SqsConsumer(IAmazonSQS sqs, IServiceProvider serviceProvider, ILogger<SqsConsumer> logger)
    {
        _sqs = sqs;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Фоновый процесс получения сообщений из очереди
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var receiveRequest = new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 20
                };

                var response = await _sqs.ReceiveMessageAsync(receiveRequest, stoppingToken);

                if (response.Messages != null)
                {
                    foreach (var message in response.Messages)
                    {
                        try
                        {
                            var creditEvent = JsonSerializer.Deserialize<CreditGeneratedEvent>(message.Body);
                            if (creditEvent != null)
                            {
                                using var scope = _serviceProvider.CreateScope();
                                var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

                                var json = JsonSerializer.Serialize(creditEvent);
                                var data = Encoding.UTF8.GetBytes(json);
                                var fileName = $"credit_{creditEvent.Id}_{DateTime.Now:yyyyMMddHHmmss}.json";

                                await storage.SaveAsync("credit-applications", fileName, data, stoppingToken);
                                _logger.LogInformation("Saved credit {Id} to S3", creditEvent.Id);
                            }

                            await _sqs.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing message");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving messages");
                await Task.Delay(5000, stoppingToken);
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}