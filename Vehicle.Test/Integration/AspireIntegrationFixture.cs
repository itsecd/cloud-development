using Amazon.Runtime;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

namespace Vehicle.Test.Integration;

/// <summary>
/// Общий fixture для интеграционных тестов.
/// Запускает весь AppHost (LocalStack, Redis, FileService, Server и др.)
/// через Aspire.Hosting.Testing один раз для всего тестового набора.
/// </summary>
public class AspireIntegrationFixture : IAsyncLifetime
{
    public DistributedApplication App { get; private set; } = null!;

    public IAmazonS3 S3Client { get; private set; } = null!;
    public IAmazonSimpleNotificationService SnsClient { get; private set; } = null!;
    public IAmazonSQS SqsClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Aspire_AppHost>();

        App = await builder.BuildAsync();
        await App.StartAsync();

        var cts3 = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await App.ResourceNotifications.WaitForResourceAsync("localstack",
            KnownResourceStates.Running, cts3.Token);

        var cts2 = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await App.ResourceNotifications.WaitForResourceAsync("cache",
            KnownResourceStates.Running, cts2.Token);

        var ctsFs = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await App.ResourceNotifications.WaitForResourceAsync("file-service",
            KnownResourceStates.Running, ctsFs.Token);

        var ctsBack = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await App.ResourceNotifications.WaitForResourceAsync("back-0",
            KnownResourceStates.Running, ctsBack.Token);

        // Клиенты AWS, указывающие на LocalStack
        var localstackUrl = App.GetEndpoint("localstack", "localstack").ToString();
        var creds = new BasicAWSCredentials("test", "test");

        S3Client = new AmazonS3Client(creds, new AmazonS3Config
        {
            ServiceURL = localstackUrl,
            ForcePathStyle = true
        });

        SnsClient = new AmazonSimpleNotificationServiceClient(creds,
            new AmazonSimpleNotificationServiceConfig { ServiceURL = localstackUrl });

        SqsClient = new AmazonSQSClient(creds,
            new AmazonSQSConfig { ServiceURL = localstackUrl });
    }

    public async Task DisposeAsync()
    {
        S3Client?.Dispose();
        SnsClient?.Dispose();
        SqsClient?.Dispose();
        if (App is not null)
            await App.DisposeAsync();
    }
}

/// <summary>
/// Определение xUnit-коллекции: все классы с [Collection("Aspire")]
/// разделяют один запущенный AppHost.
/// </summary>
[CollectionDefinition("Aspire")]
public class AspireIntegrationCollection : ICollectionFixture<AspireIntegrationFixture> { }
