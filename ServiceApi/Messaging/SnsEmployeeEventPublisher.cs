using System.Text.Json;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Options;
using Service.Api.Configuration;
using Service.Api.Entities;

namespace Service.Api.Messaging;

/// <summary>
/// Публикует события генерации сотрудников в SNS.
/// </summary>
public sealed class SnsEmployeeEventPublisher : IEmployeeEventPublisher, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly AwsMessagingOptions _options;
    private readonly ILogger<SnsEmployeeEventPublisher> _logger;
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly string _replicaId;
    private string? _topicArn;

    public SnsEmployeeEventPublisher(
        IOptions<AwsMessagingOptions> options,
        IConfiguration configuration,
        ILogger<SnsEmployeeEventPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
        _replicaId = configuration["ReplicaId"] ?? Environment.MachineName;

        var credentials = new BasicAWSCredentials(_options.AccessKey, _options.SecretKey);
        var config = new AmazonSimpleNotificationServiceConfig
        {
            ServiceURL = _options.ServiceUrl,
            AuthenticationRegion = _options.Region,
            UseHttp = _options.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        };

        _snsClient = new AmazonSimpleNotificationServiceClient(credentials, config);
    }

    public async Task PublishAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(employee);

        var topicArn = await EnsureTopicAsync(cancellationToken);
        var message = new EmployeeGeneratedMessage
        {
            EmployeeId = employee.Id,
            PublishedAtUtc = DateTime.UtcNow,
            ReplicaId = _replicaId,
            Payload = employee
        };

        var payload = JsonSerializer.Serialize(message, JsonOptions);

        _logger.LogInformation(
            "Publishing employee {EmployeeId} to SNS topic {TopicArn}",
            employee.Id,
            topicArn);

        await _snsClient.PublishAsync(new PublishRequest
        {
            TopicArn = topicArn,
            Subject = $"employee-{employee.Id}",
            Message = payload,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["employeeId"] = new()
                {
                    DataType = "Number",
                    StringValue = employee.Id.ToString()
                },
                ["replicaId"] = new()
                {
                    DataType = "String",
                    StringValue = _replicaId
                }
            }
        }, cancellationToken);
    }

    private async Task<string> EnsureTopicAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_topicArn))
        {
            return _topicArn;
        }

        var response = await _snsClient.CreateTopicAsync(new CreateTopicRequest
        {
            Name = _options.TopicName
        }, cancellationToken);

        _topicArn = response.TopicArn;
        return _topicArn;
    }

    public async ValueTask DisposeAsync()
    {
        _snsClient.Dispose();
        await Task.CompletedTask;
    }
}
