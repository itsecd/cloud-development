using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;
using Aspire.Hosting.Testing;
using Domain.Contracts;
using System.Net.Http.Json;
using System.Text.Json;

namespace Vehicle.Test.Integration;

/// <summary>
/// Интеграционные тесты Server через Aspire.Hosting.Testing.
/// Проверяют: генерацию контракта, кэширование и публикацию в SNS.
/// </summary>
[Collection("Aspire")]
public class ServerIntegrationTests(AspireIntegrationFixture fixture)
{
    [Fact]
    public async Task GetVehicle_ReturnsValidContract()
    {
        var client = fixture.App.CreateHttpClient("back-0");

        var response = await client.GetAsync("/contracts/vehicle?id=42");
        response.EnsureSuccessStatusCode();

        var contract = await response.Content.ReadFromJsonAsync<VehicleContractDto>();

        Assert.NotNull(contract);
        Assert.Equal(42, contract.SystemId);
        Assert.True(VehicleContractValidator.ValidateBool(contract));
    }

    [Fact]
    public async Task GetVehicle_SameId_ReturnsCachedContract()
    {
        var client = fixture.App.CreateHttpClient("back-0");

        var first  = await client.GetFromJsonAsync<VehicleContractDto>("/contracts/vehicle?id=100");
        var second = await client.GetFromJsonAsync<VehicleContractDto>("/contracts/vehicle?id=100");

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first.Vin, second.Vin);
        Assert.Equal(first.Manufacturer, second.Manufacturer);
    }

    [Fact]
    public async Task GetVehicle_DifferentIds_ReturnDifferentContracts()
    {
        var client = fixture.App.CreateHttpClient("back-0");

        var c1 = await client.GetFromJsonAsync<VehicleContractDto>("/contracts/vehicle?id=1");
        var c2 = await client.GetFromJsonAsync<VehicleContractDto>("/contracts/vehicle?id=2");

        Assert.NotNull(c1);
        Assert.NotNull(c2);
        Assert.NotEqual(c1.Vin, c2.Vin);
    }

    [Fact]
    public async Task GetVehicle_PublishesContractToSns()
    {
        const string topicName = "vehicle-contracts";
        const string queueName = "test-sqs-capture";

        var topicArn = (await fixture.SnsClient.CreateTopicAsync(topicName)).TopicArn;
        var queueUrl = (await fixture.SqsClient.CreateQueueAsync(queueName)).QueueUrl;
        var queueArn = (await fixture.SqsClient.GetQueueAttributesAsync(
            queueUrl, new List<string> { "QueueArn" })).Attributes["QueueArn"];

        await fixture.SnsClient.SubscribeAsync(new SubscribeRequest
        {
            TopicArn = topicArn,
            Protocol = "sqs",
            Endpoint = queueArn
        });

        var client = fixture.App.CreateHttpClient("back-0");
        var response = await client.GetAsync("/contracts/vehicle?id=999");
        response.EnsureSuccessStatusCode();

        await Task.Delay(TimeSpan.FromSeconds(2));

        var messages = await fixture.SqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = queueUrl,
            MaxNumberOfMessages = 1,
            WaitTimeSeconds = 5
        });

        Assert.Single(messages.Messages);

        var envelope = JsonSerializer.Deserialize<JsonElement>(messages.Messages[0].Body);
        var contractJson = envelope.GetProperty("Message").GetString();
        Assert.NotNull(contractJson);

        var published = JsonSerializer.Deserialize<VehicleContractDto>(contractJson);
        Assert.NotNull(published);
        Assert.Equal(999, published.SystemId);
        Assert.True(VehicleContractValidator.ValidateBool(published));
    }
}
