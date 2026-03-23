using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amazon.S3.Model;
using Aspire.Hosting.Testing;
using CreditApp.Domain.Entities;

namespace CreditApp.IntegrationTests;

/// <summary>
/// Интеграционные тесты для проверки корректной работы всех сервисов бекенда.
/// </summary>
public class CreditIntegrationTests(AppHostFixture fixture) : IClassFixture<AppHostFixture>
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Проверяет, что API отвечает на health check.
    /// </summary>
    [Fact]
    public async Task Api_HealthCheck_ReturnsHealthy()
    {
        using var httpClient = fixture.App.CreateHttpClient("credit-api-1");
        using var response = await httpClient.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Проверяет, что FileService отвечает на health check.
    /// </summary>
    [Fact]
    public async Task FileService_HealthCheck_ReturnsHealthy()
    {
        using var httpClient = fixture.App.CreateHttpClient("creditapp-fileservice");
        using var response = await httpClient.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Проверяет, что запрос через Gateway возвращает корректную кредитную заявку.
    /// </summary>
    [Fact]
    public async Task Gateway_GetCredit_ReturnsValidCreditApplication()
    {
        using var httpClient = fixture.App.CreateHttpClient("creditapp-gateway", "https");
        using var response = await httpClient.GetAsync("/api/Credit?id=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var credit = await response.Content.ReadFromJsonAsync<CreditApplication>(_jsonOptions);
        Assert.NotNull(credit);
        Assert.Equal(1, credit.Id);
        Assert.False(string.IsNullOrEmpty(credit.CreditType));
        Assert.True(credit.RequestedAmount > 0);
        Assert.True(credit.TermMonths > 0);
        Assert.True(credit.InterestRate > 0);
    }

    /// <summary>
    /// Проверяет, что повторный запрос возвращает закэшированные данные из Redis.
    /// </summary>
    [Fact]
    public async Task Gateway_RepeatedRequests_ReturnsCachedData()
    {
        var testId = Random.Shared.Next(1, 100000);
        using var httpClient = fixture.App.CreateHttpClient("creditapp-gateway", "https");

        using var response1 = await httpClient.GetAsync($"/api/Credit?id={testId}");
        response1.EnsureSuccessStatusCode();
        var content1 = await response1.Content.ReadAsStringAsync();

        using var response2 = await httpClient.GetAsync($"/api/Credit?id={testId}");
        response2.EnsureSuccessStatusCode();
        var content2 = await response2.Content.ReadAsStringAsync();

        Assert.Equal(content1, content2);
    }

    /// <summary>
    /// Проверяет маршрутизацию Gateway к API-репликам.
    /// </summary>
    [Fact]
    public async Task Gateway_MultipleRequests_AllSucceed()
    {
        using var httpClient = fixture.App.CreateHttpClient("creditapp-gateway", "https");

        var tasks = Enumerable.Range(1, 6)
            .Select(i => httpClient.GetAsync($"/api/Credit?id={i + 300}"));

        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
    }

    /// <summary>
    /// Проверяет сквозной сценарий: Gateway → API → SNS → FileService → S3.
    /// </summary>
    [Fact]
    public async Task GetCredit_FileServiceSavesToS3()
    {
        var id = Random.Shared.Next(100000, 200000);

        using var httpClient = fixture.App.CreateHttpClient("creditapp-gateway", "https");
        using var response = await httpClient.GetAsync($"/api/Credit?id={id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var objects = await fixture.WaitForS3ObjectAsync($"credit-applications/{id}_");

        Assert.NotEmpty(objects);

        var getResponse = await fixture.S3Client.GetObjectAsync("credit-files", objects.First().Key);
        using var reader = new StreamReader(getResponse.ResponseStream);
        var json = await reader.ReadToEndAsync();
        var savedCredit = JsonSerializer.Deserialize<CreditApplication>(json, _jsonOptions);

        Assert.NotNull(savedCredit);
        Assert.Equal(id, savedCredit.Id);
    }

    /// <summary>
    /// Проверяет, что разные id возвращают разные кредитные заявки.
    /// </summary>
    [Fact]
    public async Task Gateway_DifferentIds_ReturnDifferentApplications()
    {
        using var httpClient = fixture.App.CreateHttpClient("creditapp-gateway", "https");

        var credit1 = await httpClient.GetFromJsonAsync<CreditApplication>("/api/Credit?id=501", _jsonOptions);
        var credit2 = await httpClient.GetFromJsonAsync<CreditApplication>("/api/Credit?id=502", _jsonOptions);

        Assert.NotNull(credit1);
        Assert.NotNull(credit2);
        Assert.Equal(501, credit1.Id);
        Assert.Equal(502, credit2.Id);
    }

    /// <summary>
    /// Проверяет, что все обязательные поля заявки заполнены корректно.
    /// </summary>
    [Fact]
    public async Task Gateway_GetCredit_AllFieldsPopulated()
    {
        var id = Random.Shared.Next(200000, 300000);
        using var httpClient = fixture.App.CreateHttpClient("creditapp-gateway", "https");

        var credit = await httpClient.GetFromJsonAsync<CreditApplication>($"/api/Credit?id={id}", _jsonOptions);

        Assert.NotNull(credit);
        Assert.Equal(id, credit.Id);
        Assert.False(string.IsNullOrEmpty(credit.CreditType));
        Assert.False(string.IsNullOrEmpty(credit.Status));
        Assert.True(credit.RequestedAmount > 0);
        Assert.True(credit.TermMonths > 0);
        Assert.True(credit.InterestRate > 0);
        Assert.NotEqual(default, credit.SubmissionDate);
    }

    /// <summary>
    /// Проверяет, что файл в S3 содержит все поля кредитной заявки.
    /// </summary>
    [Fact]
    public async Task GetCredit_S3FileContainsAllFields()
    {
        var id = Random.Shared.Next(300000, 400000);

        using var httpClient = fixture.App.CreateHttpClient("creditapp-gateway", "https");
        var originalCredit = await httpClient.GetFromJsonAsync<CreditApplication>($"/api/Credit?id={id}", _jsonOptions);
        Assert.NotNull(originalCredit);

        var objects = await fixture.WaitForS3ObjectAsync($"credit-applications/{id}_");
        Assert.NotEmpty(objects);

        var getResponse = await fixture.S3Client.GetObjectAsync("credit-files", objects.First().Key);
        using var reader = new StreamReader(getResponse.ResponseStream);
        var json = await reader.ReadToEndAsync();
        var savedCredit = JsonSerializer.Deserialize<CreditApplication>(json, _jsonOptions);

        Assert.NotNull(savedCredit);
        Assert.Equal(originalCredit.Id, savedCredit.Id);
        Assert.Equal(originalCredit.CreditType, savedCredit.CreditType);
        Assert.Equal(originalCredit.RequestedAmount, savedCredit.RequestedAmount);
        Assert.Equal(originalCredit.TermMonths, savedCredit.TermMonths);
        Assert.Equal(originalCredit.InterestRate, savedCredit.InterestRate);
        Assert.Equal(originalCredit.Status, savedCredit.Status);
    }

    /// <summary>
    /// Проверяет, что повторный запрос (cache hit) не создаёт дубликат файла в S3.
    /// </summary>
    [Fact]
    public async Task GetCredit_CacheHit_DoesNotDuplicateS3File()
    {
        var id = Random.Shared.Next(400000, 500000);

        using var httpClient = fixture.App.CreateHttpClient("creditapp-gateway", "https");

        await httpClient.GetAsync($"/api/Credit?id={id}");
        var objectsAfterFirst = await fixture.WaitForS3ObjectAsync($"credit-applications/{id}_");
        Assert.NotEmpty(objectsAfterFirst);

        await httpClient.GetAsync($"/api/Credit?id={id}");
        await Task.Delay(TimeSpan.FromSeconds(5));

        var listResponse = await fixture.S3Client.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = "credit-files",
            Prefix = $"credit-applications/{id}_"
        });

        Assert.Single(listResponse.S3Objects);
    }
}
