using Amazon.Runtime;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Testcontainers.LocalStack;
using Testcontainers.Redis;

namespace Vehicle.Test.Integration;

/// <summary>
/// Базовый класс для интеграционных тестов.
/// Поднимает LocalStack (S3 + SNS + SQS) и Redis через Testcontainers.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected LocalStackContainer LocalStack { get; private set; } = null!;
    protected RedisContainer Redis { get; private set; } = null!;
    protected IAmazonS3 S3Client { get; private set; } = null!;
    protected IAmazonSimpleNotificationService SnsClient { get; private set; } = null!;
    protected IAmazonSQS SqsClient { get; private set; } = null!;

    protected string LocalStackUrl => LocalStack.GetConnectionString();
    protected string RedisConnectionString => Redis.GetConnectionString();

    public virtual async Task InitializeAsync()
    {
        LocalStack = new LocalStackBuilder()
            .WithEnvironment("SERVICES", "s3,sns,sqs")
            .WithEnvironment("DEFAULT_REGION", "us-east-1")
            .Build();

        Redis = new RedisBuilder().Build();

        await Task.WhenAll(
            LocalStack.StartAsync(),
            Redis.StartAsync());

        var credentials = new BasicAWSCredentials("test", "test");

        S3Client = new AmazonS3Client(credentials, new AmazonS3Config
        {
            ServiceURL = LocalStackUrl,
            ForcePathStyle = true
        });

        SnsClient = new AmazonSimpleNotificationServiceClient(credentials,
            new AmazonSimpleNotificationServiceConfig { ServiceURL = LocalStackUrl });

        SqsClient = new AmazonSQSClient(credentials,
            new AmazonSQSConfig { ServiceURL = LocalStackUrl });
    }

    public virtual async Task DisposeAsync()
    {
        S3Client.Dispose();
        SnsClient.Dispose();
        SqsClient.Dispose();
        await Task.WhenAll(
            LocalStack.DisposeAsync().AsTask(),
            Redis.DisposeAsync().AsTask());
    }
}
