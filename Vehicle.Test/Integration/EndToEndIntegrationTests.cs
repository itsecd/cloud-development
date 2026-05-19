using Amazon.S3.Model;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;
using Aspire.Hosting.Testing;
using Domain.Contracts;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Vehicle.Test.Integration;

/// <summary>
/// Сквозные интеграционные тесты всего бекенда.
/// Сценарий: Server генерирует контракт → публикует в SNS → FileService
/// получает сообщение → сохраняет JSON-файл в S3.
///
/// Т.к. LocalStack работает в Docker-контейнере, HTTP-доставка SNS→FileService
/// требует сетевого доступа Docker→хост. Поэтому тест верифицирует каждый
/// переход независимо: SNS-сообщение перехватывается через SQS, затем
/// передаётся в FileService напрямую через HTTP.
/// </summary>
[Collection("Aspire")]
public class EndToEndIntegrationTests(AspireIntegrationFixture fixture)
{
    [Fact]
    public async Task FullFlow_ServerRequest_ContractPublishedToSns_AndSavedInS3()
    {
        const string topicName = "vehicle-contracts";
        const string queueName = "e2e-capture-queue";
        const string bucket    = "vehicle-files";
        const int    contractId = 7777;

        // Шаг 1: SQS-очередь для перехвата SNS-сообщений
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

        // Шаг 2: Запрос к Server — контракт должен быть сгенерирован и опубликован
        var serverClient = fixture.App.CreateHttpClient("back-0");
        var serverResponse = await serverClient.GetAsync($"/contracts/vehicle?id={contractId}");
        serverResponse.EnsureSuccessStatusCode();

        var contract = await serverResponse.Content.ReadFromJsonAsync<VehicleContractDto>();
        Assert.NotNull(contract);
        Assert.Equal(contractId, contract.SystemId);
        Assert.True(VehicleContractValidator.ValidateBool(contract));

        // Шаг 3: Проверяем что SNS получил сообщение (через SQS)
        await Task.Delay(TimeSpan.FromSeconds(2));

        var sqsMessages = await fixture.SqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl          = queueUrl,
            MaxNumberOfMessages = 1,
            WaitTimeSeconds   = 5
        });

        Assert.Single(sqsMessages.Messages);

        var envelope = JsonSerializer.Deserialize<JsonElement>(sqsMessages.Messages[0].Body);
        var messageBody = envelope.GetProperty("Message").GetString()!;
        var contractFromSns = JsonSerializer.Deserialize<VehicleContractDto>(messageBody);

        Assert.NotNull(contractFromSns);
        Assert.Equal(contractId,      contractFromSns.SystemId);
        Assert.Equal(contract.Vin,    contractFromSns.Vin);

        // Шаг 4: Передаём SNS-сообщение в FileService (имитация HTTP-доставки)
        var fileClient = fixture.App.CreateHttpClient("file-service");
        var fileResponse = await fileClient.PostAsync("/sns",
            new StringContent(sqsMessages.Messages[0].Body, Encoding.UTF8, "application/json"));
        fileResponse.EnsureSuccessStatusCode();

        var fileResult = await fileResponse.Content.ReadFromJsonAsync<JsonElement>();
        var savedKey   = fileResult.GetProperty("key").GetString();
        Assert.NotNull(savedKey);

        // Шаг 5: Проверяем содержимое файла в S3
        var s3Object = await fixture.S3Client.GetObjectAsync(new GetObjectRequest
        {
            BucketName = bucket,
            Key        = savedKey
        });

        using var reader = new StreamReader(s3Object.ResponseStream);
        var saved = JsonSerializer.Deserialize<VehicleContractDto>(await reader.ReadToEndAsync());

        Assert.NotNull(saved);
        Assert.Equal(contract.SystemId,     saved.SystemId);
        Assert.Equal(contract.Vin,          saved.Vin);
        Assert.Equal(contract.Manufacturer, saved.Manufacturer);
        Assert.Equal(contract.Year,         saved.Year);
    }

    [Fact]
    public async Task FullFlow_MultipleRequests_AllContractsSavedToS3()
    {
        const string topicName = "vehicle-contracts";
        const string queueName = "e2e-multi-queue";
        const string bucket    = "vehicle-files";
        var ids = new[] { 11, 22, 33 };

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

        var serverClient = fixture.App.CreateHttpClient("back-0");

        foreach (var id in ids)
        {
            var response = await serverClient.GetFromJsonAsync<VehicleContractDto>(
                $"/contracts/vehicle?id={id}");
            Assert.NotNull(response);
        }

        // Ждём доставки всех сообщений
        await Task.Delay(TimeSpan.FromSeconds(3));

        var sqsMessages = await fixture.SqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl          = queueUrl,
            MaxNumberOfMessages = 10,
            WaitTimeSeconds   = 5
        });

        Assert.Equal(ids.Length, sqsMessages.Messages.Count);

        var fileClient = fixture.App.CreateHttpClient("file-service");

        foreach (var msg in sqsMessages.Messages)
        {
            var fileResponse = await fileClient.PostAsync("/sns",
                new StringContent(msg.Body, Encoding.UTF8, "application/json"));
            fileResponse.EnsureSuccessStatusCode();
        }

        // Проверяем S3: должны быть файлы для всех контрактов
        var listResponse = await fixture.S3Client.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = bucket,
            Prefix     = "vehicle-contracts/"
        });

        Assert.True(listResponse.S3Objects.Count >= ids.Length);
    }
}
