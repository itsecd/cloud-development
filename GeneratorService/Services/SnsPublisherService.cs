using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace GeneratorService.Services;

public sealed class SnsPublisherService : IDisposable
{
    private readonly IAmazonSimpleNotificationService _sns;
    private readonly string _topicName;
    private readonly ILogger<SnsPublisherService> _logger;
    private string? _topicArn;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SnsPublisherService(IConfiguration configuration, ILogger<SnsPublisherService> logger)
    {
        _logger = logger;
        _topicName = configuration["Sns:TopicName"] ?? "medical-patients";
        var serviceUrl = configuration["AWS:ServiceURL"] ?? "http://localhost:4566";
        _sns = new AmazonSimpleNotificationServiceClient(
            new BasicAWSCredentials("test", "test"),
            new AmazonSimpleNotificationServiceConfig { ServiceURL = serviceUrl });
    }

    public async Task PublishAsync(string message, CancellationToken ct = default)
    {
        if (_topicArn is null)
        {
            await _lock.WaitAsync(ct);
            try
            {
                _topicArn ??= (await _sns.CreateTopicAsync(_topicName, ct)).TopicArn;
            }
            finally
            {
                _lock.Release();
            }
        }

        await _sns.PublishAsync(new PublishRequest
        {
            TopicArn = _topicArn,
            Message = message
        }, ct);

        _logger.LogInformation("Published to SNS topic {TopicArn}", _topicArn);
    }

    public void Dispose()
    {
        _sns.Dispose();
        _lock.Dispose();
    }
}
