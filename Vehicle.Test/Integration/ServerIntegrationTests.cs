using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Domain.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;

namespace Vehicle.Test.Integration;

/// <summary>
/// Интеграционные тесты для Server:
/// корректность генерации контракта и публикация в SNS.
/// </summary>
public class ServerIntegrationTests : IntegrationTestBase
{
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host =>
            {
                host.ConfigureServices(services =>
                {
                    services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
                        new AmazonSimpleNotificationServiceClient(
                            new BasicAWSCredentials("test", "test"),
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
                host.UseSetting("AWS:Region", "us-east-1");
                host.UseSetting("AWS:TopicName", "test-vehicle-contracts");
            });

        _client = _factory.CreateClient();
    }

    public override async Task DisposeAsync()
    {
        _factory?.Dispose();
        await base.DisposeAsync();
    }

    [Fact]
    public async Task GetVehicle_ReturnsValidContract()
    {
        var response = await _client!.GetAsync("/contracts/vehicle?id=42");
        response.EnsureSuccessStatusCode();

        var contract = await response.Content.ReadFromJsonAsync<VehicleContractDto>();

        Assert.NotNull(contract);
        Assert.Equal(42, contract.SystemId);
        Assert.True(VehicleContractValidator.ValidateBool(contract));
    }

    [Fact]
    public async Task GetVehicle_SameId_ReturnsCachedContract()
    {
        var first = await _client!.GetFromJsonAsync<VehicleContractDto>("/contracts/vehicle?id=100");
        var second = await _client!.GetFromJsonAsync<VehicleContractDto>("/contracts/vehicle?id=100");

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first.Vin, second.Vin);
        Assert.Equal(first.Manufacturer, second.Manufacturer);
    }

    [Fact]
    public async Task GetVehicle_DifferentIds_ReturnDifferentContracts()
    {
        var contract1 = await _client!.GetFromJsonAsync<VehicleContractDto>("/contracts/vehicle?id=1");
        var contract2 = await _client!.GetFromJsonAsync<VehicleContractDto>("/contracts/vehicle?id=2");

        Assert.NotNull(contract1);
        Assert.NotNull(contract2);
        Assert.NotEqual(contract1.Vin, contract2.Vin);
    }

    [Fact]
    public async Task GetVehicle_PublishesContractToSnsTopic()
    {
        // Создаём SNS тему и подписываем SQS-очередь для перехвата сообщений
        var topicArn = (await SnsClient.CreateTopicAsync("test-vehicle-contracts")).TopicArn;
        var queueUrl = (await SqsClient.CreateQueueAsync("test-capture-queue")).QueueUrl;
        var queueArn = (await SqsClient.GetQueueAttributesAsync(queueUrl,
            new List<string> { "QueueArn" })).Attributes["QueueArn"];

        await SnsClient.SubscribeAsync(new SubscribeRequest
        {
            TopicArn = topicArn,
            Protocol = "sqs",
            Endpoint = queueArn
        });

        // Делаем запрос к Server
        var response = await _client!.GetAsync("/contracts/vehicle?id=999");
        response.EnsureSuccessStatusCode();

        // Ждём доставки сообщения
        await Task.Delay(500);

        var messages = await SqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = queueUrl,
            MaxNumberOfMessages = 1,
            WaitTimeSeconds = 3
        });

        Assert.Single(messages.Messages);

        var snsEnvelope = JsonSerializer.Deserialize<JsonElement>(messages.Messages[0].Body);
        var contractJson = snsEnvelope.GetProperty("Message").GetString();
        Assert.NotNull(contractJson);

        var contract = JsonSerializer.Deserialize<VehicleContractDto>(contractJson);
        Assert.NotNull(contract);
        Assert.Equal(999, contract.SystemId);
        Assert.True(VehicleContractValidator.ValidateBool(contract));
    }
}
