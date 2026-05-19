using Amazon.S3.Model;
using Aspire.Hosting.Testing;
using Domain.Contracts;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Vehicle.Test.Integration;

/// <summary>
/// Интеграционные тесты FileService через Aspire.Hosting.Testing.
/// Проверяют: обработку SNS-уведомлений и сохранение файлов в S3.
/// </summary>
[Collection("Aspire")]
public class FileServiceIntegrationTests(AspireIntegrationFixture fixture)
{
    private const string BucketName = "vehicle-files";

    [Fact]
    public async Task SnsNotification_SavesContractToS3()
    {
        var contract = new VehicleContractDto
        {
            SystemId    = 55555,
            Vin         = "1HGCM82633A555555",
            Manufacturer = "Honda",
            Model       = "Accord",
            Year        = 2022,
            BodyType    = "Sedan",
            FuelType    = "Gasoline",
            Color       = "Red",
            Mileage     = 12000.0,
            LastServiceDate = new DateOnly(2023, 9, 1)
        };

        var notification = new
        {
            Type     = "Notification",
            TopicArn = "arn:aws:sns:us-east-1:000000000000:vehicle-contracts",
            Subject  = "VehicleContract",
            Message  = JsonSerializer.Serialize(contract),
            MessageId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow.ToString("o")
        };

        var client = fixture.App.CreateHttpClient("file-service");
        var response = await client.PostAsync("/sns",
            new StringContent(JsonSerializer.Serialize(notification), Encoding.UTF8, "application/json"));

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var savedKey = result.GetProperty("key").GetString();
        Assert.NotNull(savedKey);
        Assert.Contains("55555", savedKey);

        // Проверяем файл в S3
        var s3Object = await fixture.S3Client.GetObjectAsync(new GetObjectRequest
        {
            BucketName = BucketName,
            Key        = savedKey
        });

        using var reader = new StreamReader(s3Object.ResponseStream);
        var json   = await reader.ReadToEndAsync();
        var saved  = JsonSerializer.Deserialize<VehicleContractDto>(json);

        Assert.NotNull(saved);
        Assert.Equal(contract.SystemId,    saved.SystemId);
        Assert.Equal(contract.Vin,         saved.Vin);
        Assert.Equal(contract.Manufacturer, saved.Manufacturer);
        Assert.Equal(contract.Mileage,     saved.Mileage);
    }

    [Fact]
    public async Task SnsEndpoint_InvalidJson_ReturnsBadRequest()
    {
        var client = fixture.App.CreateHttpClient("file-service");
        var response = await client.PostAsync("/sns",
            new StringContent("not-valid-json", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task S3_MultipleContracts_StoredWithUniqueKeys()
    {
        var ids = new[] { 2001, 2002, 2003 };
        var keys = new List<string>();
        var client = fixture.App.CreateHttpClient("file-service");

        foreach (var id in ids)
        {
            var contract = new VehicleContractDto
            {
                SystemId    = id,
                Vin         = $"1HGCM{id:D7}XXXXX",
                Manufacturer = "Toyota",
                Model       = "Camry",
                Year        = 2023,
                BodyType    = "Sedan",
                FuelType    = "Hybrid",
                Color       = "White",
                Mileage     = id * 500.0,
                LastServiceDate = new DateOnly(2024, 1, 1)
            };

            var notification = new
            {
                Type      = "Notification",
                TopicArn  = "arn:aws:sns:us-east-1:000000000000:vehicle-contracts",
                Message   = JsonSerializer.Serialize(contract),
                MessageId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow.ToString("o")
            };

            var response = await client.PostAsync("/sns",
                new StringContent(JsonSerializer.Serialize(notification), Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            keys.Add(result.GetProperty("key").GetString()!);

            await Task.Delay(50);
        }

        // Все ключи уникальны
        Assert.Equal(keys.Count, keys.Distinct().Count());

        // Каждый ключ содержит SystemId
        for (var i = 0; i < ids.Length; i++)
            Assert.Contains(ids[i].ToString(), keys[i]);
    }
}
