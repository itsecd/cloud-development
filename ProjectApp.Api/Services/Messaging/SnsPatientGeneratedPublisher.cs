using System;
using System.Text.Json;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using ProjectApp.Domain.Entities;
using ProjectApp.Domain.Messaging;

namespace ProjectApp.Api.Services.Messaging;

public sealed class SnsPatientGeneratedPublisher(
    IConfiguration configuration,
    ILogger<SnsPatientGeneratedPublisher> logger) : IPatientGeneratedPublisher, IDisposable
{
    private readonly PatientMessagingOptions _options =
        configuration.GetSection(PatientMessagingOptions.SectionName).Get<PatientMessagingOptions>() ?? new();
    private readonly SemaphoreSlim _topicLock = new(1, 1);
    private readonly IAmazonSimpleNotificationService _sns = CreateSnsClient(
        configuration.GetSection(PatientMessagingOptions.SectionName).Get<PatientMessagingOptions>() ?? new());
    private string? _topicArn;

    public async Task PublishAsync(MedicalPatient patient, CancellationToken cancellationToken = default)
    {
        try
        {
            var topicArn = await GetTopicArnAsync(cancellationToken);
            await WaitForSubscriptionAsync(topicArn, cancellationToken);

            var message = new PatientGeneratedMessage
            {
                Patient = patient,
                GeneratedAt = DateTimeOffset.UtcNow
            };

            await _sns.PublishAsync(new PublishRequest
            {
                TopicArn = topicArn,
                Message = JsonSerializer.Serialize(message)
            }, cancellationToken);

            logger.LogInformation("Published patient {Id} to SNS topic {TopicName}", patient.Id, _options.TopicName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish patient {Id} to SNS", patient.Id);
        }
    }

    private async Task<string> GetTopicArnAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_topicArn))
        {
            return _topicArn;
        }

        await _topicLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_topicArn))
            {
                return _topicArn;
            }

            var response = await _sns.CreateTopicAsync(_options.TopicName, cancellationToken);
            _topicArn = response.TopicArn;
            return _topicArn;
        }
        finally
        {
            _topicLock.Release();
        }
    }

    private async Task WaitForSubscriptionAsync(string topicArn, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);

        while (DateTimeOffset.UtcNow < deadline)
        {
            var response = await _sns.ListSubscriptionsByTopicAsync(topicArn, cancellationToken);
            if (response.Subscriptions.Any(subscription =>
                    !string.Equals(subscription.SubscriptionArn, "PendingConfirmation", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        logger.LogWarning("SNS topic {TopicName} has no confirmed subscriptions; publishing anyway", _options.TopicName);
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

    public void Dispose()
    {
        _sns.Dispose();
        _topicLock.Dispose();
    }
}
