using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using File.Service.Configuration;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace File.Service.Infrastructure;

public sealed class FileExportReadinessProbe
{
    private readonly AwsStorageOptions _options;
    private readonly ILogger<FileExportReadinessProbe> _logger;

    public FileExportReadinessProbe(
        IOptions<AwsStorageOptions> options,
        ILogger<FileExportReadinessProbe> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> IsReadyAsync(CancellationToken cancellationToken)
    {
        var serviceUrl = GetStringProperty(_options, "ServiceUrl", "AwsServiceUrl");
        var region = GetStringProperty(_options, "Region", "RegionName") ?? "us-east-1";
        var accessKey = GetStringProperty(_options, "AccessKey", "AccessKeyId") ?? "test";
        var secretKey = GetStringProperty(_options, "SecretKey", "SecretAccessKey") ?? "test";
        var topicName = GetStringProperty(_options, "TopicName", "SnsTopicName");
        var queueName = GetStringProperty(_options, "QueueName", "SqsQueueName");

        if (string.IsNullOrWhiteSpace(serviceUrl) ||
            string.IsNullOrWhiteSpace(topicName) ||
            string.IsNullOrWhiteSpace(queueName))
        {
            _logger.LogDebug(
                "File export readiness is false because required AWS settings are missing. ServiceUrl={ServiceUrl}, TopicName={TopicName}, QueueName={QueueName}",
                serviceUrl,
                topicName,
                queueName);

            return false;
        }

        try
        {
            var credentials = new BasicAWSCredentials(accessKey, secretKey);

            using var sns = new AmazonSimpleNotificationServiceClient(
                credentials,
                new AmazonSimpleNotificationServiceConfig
                {
                    ServiceURL = serviceUrl,
                    AuthenticationRegion = region
                });

            using var sqs = new AmazonSQSClient(
                credentials,
                new AmazonSQSConfig
                {
                    ServiceURL = serviceUrl,
                    AuthenticationRegion = region
                });

            var topicArn = await FindTopicArnAsync(sns, topicName, cancellationToken);
            if (string.IsNullOrWhiteSpace(topicArn))
            {
                return false;
            }

            var queueUrlResponse = await sqs.GetQueueUrlAsync(
                new GetQueueUrlRequest
                {
                    QueueName = queueName
                },
                cancellationToken);

            if (string.IsNullOrWhiteSpace(queueUrlResponse.QueueUrl))
            {
                return false;
            }

            var subscriptionExists = await HasQueueSubscriptionAsync(
                sns,
                topicArn,
                queueName,
                cancellationToken);

            return subscriptionExists;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "File export readiness probe failed.");
            return false;
        }
    }

    private static async Task<string?> FindTopicArnAsync(
        IAmazonSimpleNotificationService sns,
        string topicName,
        CancellationToken cancellationToken)
    {
        string? nextToken = null;

        do
        {
            var response = await sns.ListTopicsAsync(
                new ListTopicsRequest
                {
                    NextToken = nextToken
                },
                cancellationToken);

            var topic = response.Topics.FirstOrDefault(t =>
                !string.IsNullOrWhiteSpace(t.TopicArn) &&
                t.TopicArn.EndsWith($":{topicName}", StringComparison.OrdinalIgnoreCase));

            if (topic is not null)
            {
                return topic.TopicArn;
            }

            nextToken = response.NextToken;
        }
        while (!string.IsNullOrWhiteSpace(nextToken));

        return null;
    }

    private static async Task<bool> HasQueueSubscriptionAsync(
        IAmazonSimpleNotificationService sns,
        string topicArn,
        string queueName,
        CancellationToken cancellationToken)
    {
        string? nextToken = null;

        do
        {
            var response = await sns.ListSubscriptionsByTopicAsync(
                new ListSubscriptionsByTopicRequest
                {
                    TopicArn = topicArn,
                    NextToken = nextToken
                },
                cancellationToken);

            var exists = response.Subscriptions.Any(subscription =>
                !string.IsNullOrWhiteSpace(subscription.Endpoint) &&
                EndpointMatchesQueue(subscription.Endpoint, queueName));

            if (exists)
            {
                return true;
            }

            nextToken = response.NextToken;
        }
        while (!string.IsNullOrWhiteSpace(nextToken));

        return false;
    }

    private static bool EndpointMatchesQueue(string endpoint, string queueName)
    {
        return endpoint.EndsWith($"/{queueName}", StringComparison.OrdinalIgnoreCase) ||
               endpoint.Contains($":{queueName}", StringComparison.OrdinalIgnoreCase) ||
               endpoint.Contains(queueName, StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetStringProperty(object source, params string[] names)
    {
        var type = source.GetType();

        foreach (var name in names)
        {
            var property = type.GetProperty(
                name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (property?.PropertyType != typeof(string))
            {
                continue;
            }

            var value = property.GetValue(source) as string;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}