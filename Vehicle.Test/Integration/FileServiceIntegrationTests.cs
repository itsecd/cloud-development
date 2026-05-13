extern alias FileServiceApp;

using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleNotificationService;
using Domain.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using IS3FileStorageService = FileServiceApp::FileService.Services.IS3FileStorageService;
using FileProgram = FileServiceApp::Program;

namespace Vehicle.Test.Integration;

/// <summary>
/// Интеграционные тесты для FileService:
/// обработка SNS-уведомлений и сохранение файлов в S3.
/// </summary>
public class FileServiceIntegrationTests : IntegrationTestBase
{
    private const string TestBucket = "test-vehicle-files";
    private const string TestTopic = "test-vehicle-contracts";

    private WebApplicationFactory<FileProgram>? _factory;
    private HttpClient? _client;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _factory = new WebApplicationFactory<FileProgram>()
            .WithWebHostBuilder(host =>
            {
                host.ConfigureServices(services =>
                {
                    var credentials = new BasicAWSCredentials("test", "test");

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
                host.UseSetting("AWS:BucketName", TestBucket);
                host.UseSetting("AWS:TopicName", TestTopic);
                host.UseSetting("FileService:SnsCallbackUrl", "");
            });

        _client = _factory.CreateClient();

        var storageService = _factory.Services.GetRequiredService<IS3FileStorageService>();
        await storageService.EnsureBucketExistsAsync();
    }

    public override async Task DisposeAsync()
    {
        _factory?.Dispose();
        await base.DisposeAsync();
    }

    [Fact]
    public async Task SnsEndpoint_SubscriptionConfirmation_ReturnsOk()
    {
        var notification = new
        {
            Type = "SubscriptionConfirmation",
            TopicArn = $"arn:aws:sns:us-east-1:000000000000:{TestTopic}",
            Token = "test-token",
            SubscribeURL = $"{LocalStackUrl}/?Action=ConfirmSubscription&TopicArn=arn%3Aaws%3Asns%3Aus-east-1%3A000000000000%3A{TestTopic}&Token=test-token",
            MessageId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow.ToString("o")
        };

        var content = new StringContent(
            JsonSerializer.Serialize(notification),
            Encoding.UTF8, "application/json");

        var response = await _client!.PostAsync("/sns", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SnsEndpoint_Notification_SavesContractToS3()
    {
        var contract = new VehicleContractDto
        {
            SystemId = 12345,
            Vin = "1HGCM82633A123456",
            Manufacturer = "Honda",
            Model = "Accord",
            Year = 2020,
            BodyType = "Sedan",
            FuelType = "Gasoline",
            Color = "Blue",
            Mileage = 45000.5,
            LastServiceDate = new DateOnly(2023, 6, 15)
        };

        var notification = new
        {
            Type = "Notification",
            TopicArn = $"arn:aws:sns:us-east-1:000000000000:{TestTopic}",
            Subject = "VehicleContract",
            Message = JsonSerializer.Serialize(contract),
            MessageId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow.ToString("o")
        };

        var content = new StringContent(
            JsonSerializer.Serialize(notification),
            Encoding.UTF8, "application/json");

        var response = await _client!.PostAsync("/sns", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var savedKey = result.GetProperty("Key").GetString();
        Assert.NotNull(savedKey);
        Assert.Contains("12345", savedKey);

        // Проверяем файл в S3
        var s3Object = await S3Client.GetObjectAsync(new GetObjectRequest
        {
            BucketName = TestBucket,
            Key = savedKey
        });

        using var reader = new StreamReader(s3Object.ResponseStream);
        var fileContent = await reader.ReadToEndAsync();
        var savedContract = JsonSerializer.Deserialize<VehicleContractDto>(fileContent);

        Assert.NotNull(savedContract);
        Assert.Equal(contract.SystemId, savedContract.SystemId);
        Assert.Equal(contract.Vin, savedContract.Vin);
        Assert.Equal(contract.Manufacturer, savedContract.Manufacturer);
        Assert.Equal(contract.Mileage, savedContract.Mileage);
    }

    [Fact]
    public async Task SnsEndpoint_InvalidJson_ReturnsBadRequest()
    {
        var content = new StringContent("not-valid-json", Encoding.UTF8, "application/json");
        var response = await _client!.PostAsync("/sns", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task S3_MultipleContracts_SavedWithUniqueKeys()
    {
        var contractIds = new[] { 1001, 1002, 1003 };
        var savedKeys = new List<string>();

        foreach (var id in contractIds)
        {
            var contract = new VehicleContractDto
            {
                SystemId = id,
                Vin = $"VIN{id:D12}ABCDE",
                Manufacturer = "Toyota",
                Model = "Camry",
                Year = 2021,
                BodyType = "Sedan",
                FuelType = "Hybrid",
                Color = "White",
                Mileage = id * 1000.0,
                LastServiceDate = new DateOnly(2023, 1, 1)
            };

            var notification = new
            {
                Type = "Notification",
                TopicArn = $"arn:aws:sns:us-east-1:000000000000:{TestTopic}",
                Message = JsonSerializer.Serialize(contract),
                MessageId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow.ToString("o")
            };

            var response = await _client!.PostAsync("/sns",
                new StringContent(JsonSerializer.Serialize(notification), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            savedKeys.Add(result.GetProperty("Key").GetString()!);

            await Task.Delay(10);
        }

        Assert.Equal(savedKeys.Count, savedKeys.Distinct().Count());
        for (var i = 0; i < contractIds.Length; i++)
            Assert.Contains(contractIds[i].ToString(), savedKeys[i]);
    }
}
