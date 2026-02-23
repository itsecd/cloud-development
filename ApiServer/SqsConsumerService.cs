using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Contracts;

namespace ApiServer;

public class SqsConsumerService : BackgroundService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly EmployeeStore _store;
    private readonly ILogger<SqsConsumerService> _logger;
    private readonly string _queueUrl;

    public SqsConsumerService(
        IAmazonSQS sqsClient,
        EmployeeStore store,
        ILogger<SqsConsumerService> logger,
        IConfiguration configuration)
    {
        _sqsClient = sqsClient;
        _store = store;
        _logger = logger;
        _queueUrl = configuration["Sqs:QueueUrl"]
            ?? "http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/employee-queue";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SQS Consumer started. Queue: {QueueUrl}", _queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var request = new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 5
                };

                var response = await _sqsClient.ReceiveMessageAsync(request, stoppingToken);

                foreach (var message in response.Messages)
                {
                    try
                    {
                        _logger.LogInformation("Received SQS message: {MessageId}", message.MessageId);

                        // SNS wraps the message in an envelope
                        JsonDocument snsEnvelope;
                        string actualMessage;

                        try
                        {
                            snsEnvelope = JsonDocument.Parse(message.Body);
                            if (snsEnvelope.RootElement.TryGetProperty("Message", out var msgProp))
                            {
                                actualMessage = msgProp.GetString() ?? message.Body;
                            }
                            else
                            {
                                actualMessage = message.Body;
                            }
                        }
                        catch
                        {
                            actualMessage = message.Body;
                        }

                        var employeeMessage = JsonSerializer.Deserialize<EmployeeMessage>(actualMessage);

                        if (employeeMessage?.Employees != null)
                        {
                            await _store.AddEmployeesAsync(employeeMessage.Employees);
                            _logger.LogInformation(
                                "Stored {Count} employees from SQS",
                                employeeMessage.Employees.Count);
                        }

                        await _sqsClient.DeleteMessageAsync(_queueUrl,
                            message.ReceiptHandle, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing SQS message");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving SQS messages");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
