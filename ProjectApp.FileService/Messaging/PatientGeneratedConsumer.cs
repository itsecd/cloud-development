using System.Text.Json;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Collections.Generic;
using ProjectApp.Domain.Messaging;
using ProjectApp.FileService.ObjectStorage;

namespace ProjectApp.FileService.Messaging;

public sealed class PatientGeneratedConsumer(
    IConfiguration configuration,
    IPatientFileStorage fileStorage,
    ILogger<PatientGeneratedConsumer> logger) : BackgroundService
{
    private readonly PatientMessagingOptions _options =
        configuration.GetSection(PatientMessagingOptions.SectionName).Get<PatientMessagingOptions>() ?? new();
    private readonly IAmazonSimpleNotificationService _sns =
        CreateSnsClient(configuration.GetSection(PatientMessagingOptions.SectionName).Get<PatientMessagingOptions>() ?? new());
    private readonly IAmazonSQS _sqs =
        CreateSqsClient(configuration.GetSection(PatientMessagingOptions.SectionName).Get<PatientMessagingOptions>() ?? new());

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var queueUrl = await InitializeSubscriptionAsync(stoppingToken);
                logger.LogInformation(
                    "Started SNS consumer for topic {TopicName} and queue {QueueName}",
                    _options.TopicName,
                    _options.QueueName);

                await PollQueueAsync(queueUrl, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "SNS consumer failed. Retrying in 5 seconds");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task<string> InitializeSubscriptionAsync(CancellationToken cancellationToken)
    {
        var topic = await _sns.CreateTopicAsync(_options.TopicName, cancellationToken);
        var queue = await _sqs.CreateQueueAsync(_options.QueueName, cancellationToken);
        var attributes = await _sqs.GetQueueAttributesAsync(new GetQueueAttributesRequest
        {
            QueueUrl = queue.QueueUrl,
            AttributeNames = ["QueueArn"]
        }, cancellationToken);

        var queueArn = attributes.QueueARN;
        var policy = $$"""
            {
              "Version": "2012-10-17",
              "Statement": [
                {
                  "Effect": "Allow",
                  "Principal": "*",
                  "Action": "sqs:SendMessage",
                  "Resource": "{{queueArn}}",
                  "Condition": {
                    "ArnEquals": {
                      "aws:SourceArn": "{{topic.TopicArn}}"
                    }
                  }
                }
              ]
            }
            """;

        await _sqs.SetQueueAttributesAsync(new SetQueueAttributesRequest
        {
            QueueUrl = queue.QueueUrl,
            Attributes = new Dictionary<string, string>
            {
                ["Policy"] = policy
            }
        }, cancellationToken);

        await _sns.SubscribeAsync(new SubscribeRequest
        {
            TopicArn = topic.TopicArn,
            Protocol = "sqs",
            Endpoint = queueArn,
            Attributes = new Dictionary<string, string>
            {
                ["RawMessageDelivery"] = "true"
            }
        }, cancellationToken);

        return queue.QueueUrl;
    }

    private async Task PollQueueAsync(string queueUrl, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = _options.WaitTimeSeconds
            }, cancellationToken);

            foreach (var message in response.Messages)
            {
                await ProcessMessageAsync(queueUrl, message, cancellationToken);
            }
        }
    }

    private async Task ProcessMessageAsync(string queueUrl, Message sqsMessage, CancellationToken cancellationToken)
    {
        try
        {
            var message = JsonSerializer.Deserialize<PatientGeneratedMessage>(sqsMessage.Body);
            if (message is null)
            {
                throw new JsonException("Patient generated message is empty");
            }

            await fileStorage.SaveAsync(message, cancellationToken);
            await _sqs.DeleteMessageAsync(queueUrl, sqsMessage.ReceiptHandle, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process generated patient message");
        }
    }

    private static AmazonSimpleNotificationServiceClient CreateSnsClient(PatientMessagingOptions options)
    {
        var config = new AmazonSimpleNotificationServiceConfig
        {
            AuthenticationRegion = options.Region
        };

        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            config.ServiceURL = options.ServiceUrl;
        }
        else
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
        }

        return new AmazonSimpleNotificationServiceClient(
            new BasicAWSCredentials(options.AccessKey, options.SecretKey),
            config);
    }

    private static AmazonSQSClient CreateSqsClient(PatientMessagingOptions options)
    {
        var config = new AmazonSQSConfig
        {
            AuthenticationRegion = options.Region
        };

        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            config.ServiceURL = options.ServiceUrl;
        }
        else
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
        }

        return new AmazonSQSClient(
            new BasicAWSCredentials(options.AccessKey, options.SecretKey),
            config);
    }

    public override void Dispose()
    {
        _sns.Dispose();
        _sqs.Dispose();
        base.Dispose();
    }
}
