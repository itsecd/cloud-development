extern alias FileServiceApp;

using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Domain.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using IS3FileStorageService = FileServiceApp::FileService.Services.IS3FileStorageService;
using FileProgram = FileServiceApp::Program;

namespace Vehicle.Test.Integration;

/// <summary>
/// Сквозные интеграционные тесты всего бекенда:
/// Server → SNS → FileService → S3.
///
/// Т.к. LocalStack работает в Docker-контейнере, прямая HTTP-доставка SNS→FileService
/// недоступна без настройки сети. Поэтому тест проверяет каждый переход независимо:
///   1. Server публикует контракт в SNS (верифицируется через SQS-подписку).
///   2. FileService сохраняет контракт из SNS-сообщения в S3 (прямой вызов через HTTP).
///   3. S3 содержит корректные данные.
/// </summary>
public class EndToEndIntegrationTests : IntegrationTestBase
{
    private const string SnsTopic = "e2e-vehicle-contracts";
    private const string SqsQueue = "e2e-capture-queue";
    private const string S3Bucket = "e2e-vehicle-files";

    private WebApplicationFactory<Program>? _serverFactory;
    private WebApplicationFactory<FileProgram>? _fileFactory;
    private HttpClient? _serverClient;
    private HttpClient? _fileClient;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        var credentials = new BasicAWSCredentials("test", "test");

        _serverFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host =>
            {
                host.ConfigureServices(services =>
                {
                    services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
                        new AmazonSimpleNotificationServiceClient(credentials,
                            new AmazonSimpleNotificationServiceConfig
                            {
                                ServiceURL = LocalStackUrl
                            }));

                    services.AddStackExchangeRedisCache(options =>
                        options.Configuration = RedisConnectionString);
                });

                host.UseSetting("ClientAddress", "http://localhost");
                host.UseSetting("CacheSettings:VehicleContractExpirationMinutes", "1");
                host.UseSetting("AWS:ServiceURL", LocalStackUrl);
                host.UseSetting("AWS:TopicName", SnsTopic);
            });

        _fileFactory = new WebApplicationFactory<FileProgram>()
            .WithWebHostBuilder(host =>
            {
                host.ConfigureServices(services =>
                {
                    services.AddSingleton<IAmazonS3>(_ =>
                        new AmazonS3Client(credentials, new AmazonS3Config
                        {
                            ServiceURL = LocalStackUrl,
                            ForcePathStyle = true
                        }));

                    services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
                        new AmazonSimpleNotificationServiceClient(credentials,
                            new AmazonSimpleNotificationServiceConfig
                            {
                                ServiceURL = LocalStackUrl
                            }));
                });

                host.UseSetting("AWS:ServiceURL", LocalStackUrl);
                host.UseSetting("AWS:BucketName", S3Bucket);
                host.UseSetting("AWS:TopicName", SnsTopic);
                host.UseSetting("FileService:SnsCallbackUrl", "");
            });

        _serverClient = _serverFactory.CreateClient();
        _fileClient = _fileFactory.CreateClient();

        var storageService = _fileFactory.Services.GetRequiredService<IS3FileStorageService>();
        await storageService.EnsureBucketExistsAsync();
    }

    public override async Task DisposeAsync()
    {
        _serverFactory?.Dispose();
        _fileFactory?.Dispose();
        await base.DisposeAsync();
    }

    [Fact]
    public async Task FullFlow_ContractRequest_PublishedToSns_AndSavedToS3()
    {
        // Шаг 1: Создаём SNS тему и SQS-очередь для перехвата сообщений
        var topicArn = (await SnsClient.CreateTopicAsync(SnsTopic)).TopicArn;
        var queueUrl = (await SqsClient.CreateQueueAsync(SqsQueue)).QueueUrl;
        var queueArn = (await SqsClient.GetQueueAttributesAsync(queueUrl,
            new List<string> { "QueueArn" })).Attributes["QueueArn"];

        await SnsClient.SubscribeAsync(new SubscribeRequest
        {
            TopicArn = topicArn,
            Protocol = "sqs",
            Endpoint = queueArn
        });

        // Шаг 2: Запрашиваем контракт у Server
        const int contractId = 7777;
        var serverResponse = await _serverClient!.GetAsync($"/contracts/vehicle?id={contractId}");
        serverResponse.EnsureSuccessStatusCode();

        var contract = await serverResponse.Content.ReadFromJsonAsync<VehicleContractDto>();
        Assert.NotNull(contract);
        Assert.Equal(contractId, contract.SystemId);
        Assert.True(VehicleContractValidator.ValidateBool(contract));

        // Шаг 3: Проверяем, что SNS получил сообщение (через SQS)
        await Task.Delay(500);

        var sqsMessages = await SqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = queueUrl,
            MaxNumberOfMessages = 1,
            WaitTimeSeconds = 3
        });

        Assert.Single(sqsMessages.Messages);

        var snsEnvelope = JsonSerializer.Deserialize<JsonElement>(sqsMessages.Messages[0].Body);
        var contractInMessage = JsonSerializer.Deserialize<VehicleContractDto>(
            snsEnvelope.GetProperty("Message").GetString()!);

        Assert.NotNull(contractInMessage);
        Assert.Equal(contractId, contractInMessage.SystemId);
        Assert.Equal(contract.Vin, contractInMessage.Vin);

        // Шаг 4: Передаём SNS-сообщение в FileService (имитация HTTP-доставки)
        var snsNotificationBody = sqsMessages.Messages[0].Body;
        var fileResponse = await _fileClient!.PostAsync("/sns",
            new StringContent(snsNotificationBody, Encoding.UTF8, "application/json"));
        fileResponse.EnsureSuccessStatusCode();

        var fileResult = await fileResponse.Content.ReadFromJsonAsync<JsonElement>();
        var savedKey = fileResult.GetProperty("Key").GetString();
        Assert.NotNull(savedKey);

        // Шаг 5: Проверяем содержимое файла в S3
        var s3Object = await S3Client.GetObjectAsync(new GetObjectRequest
        {
            BucketName = S3Bucket,
            Key = savedKey
        });

        using var reader = new StreamReader(s3Object.ResponseStream);
        var fileJson = await reader.ReadToEndAsync();
        var savedContract = JsonSerializer.Deserialize<VehicleContractDto>(fileJson);

        Assert.NotNull(savedContract);
        Assert.Equal(contract.SystemId, savedContract.SystemId);
        Assert.Equal(contract.Vin, savedContract.Vin);
        Assert.Equal(contract.Manufacturer, savedContract.Manufacturer);
        Assert.Equal(contract.Year, savedContract.Year);
    }

    [Fact]
    public async Task FullFlow_MultipleRequests_AllSavedToS3()
    {
        var topicArn = (await SnsClient.CreateTopicAsync($"{SnsTopic}-multi")).TopicArn;
        var queueUrl = (await SqsClient.CreateQueueAsync($"{SqsQueue}-multi")).QueueUrl;
        var queueArn = (await SqsClient.GetQueueAttributesAsync(queueUrl,
            new List<string> { "QueueArn" })).Attributes["QueueArn"];

        await SnsClient.SubscribeAsync(new SubscribeRequest
        {
            TopicArn = topicArn,
            Protocol = "sqs",
            Endpoint = queueArn
        });

        var ids = new[] { 11, 22, 33 };

        foreach (var id in ids)
        {
            var response = await _serverClient!.GetFromJsonAsync<VehicleContractDto>(
                $"/contracts/vehicle?id={id}");
            Assert.NotNull(response);
        }

        await Task.Delay(1000);

        var sqsMessages = await SqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = queueUrl,
            MaxNumberOfMessages = 10,
            WaitTimeSeconds = 3
        });

        Assert.Equal(ids.Length, sqsMessages.Messages.Count);

        foreach (var msg in sqsMessages.Messages)
        {
            var fileResponse = await _fileClient!.PostAsync("/sns",
                new StringContent(msg.Body, Encoding.UTF8, "application/json"));
            fileResponse.EnsureSuccessStatusCode();
        }

        var listResponse = await S3Client.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = S3Bucket,
            Prefix = "vehicle-contracts/"
        });

        Assert.True(listResponse.S3Objects.Count >= ids.Length);
    }
}
