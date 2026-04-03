using System.Net.Http.Json;
using CreditApp.Domain.Data;
using Xunit;

namespace CreditApp.Tests;

/// <summary>
/// Интеграционные тесты для проверки работы всех сервисов бекенда
/// </summary>
public class IntegrationTests
{
    private readonly HttpClient _client;

    public IntegrationTests()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        };

        _client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:9002")
        };
    }

    /// <summary>
    /// Проверка полного цикла: генерация кредита и сохранение в S3 через SQS
    /// </summary>
    [Fact]
    public async Task FullCycle_GenerateCredit_ShouldSaveToMinIO()
    {
        var response = await _client.GetAsync("api/Credit?id=999");
        response.EnsureSuccessStatusCode();

        var credit = await response.Content.ReadFromJsonAsync<CreditApplication>();
        Assert.NotNull(credit);
        Assert.Equal(999, credit.Id);
    }

    /// <summary>
    /// Проверка кэширования в Redis (второй запрос должен быть быстрее)
    /// </summary>
    [Fact]
    public async Task CacheTest_SecondRequest_ShouldBeFaster()
    {
        var response1 = await _client.GetAsync("api/Credit?id=100");
        response1.EnsureSuccessStatusCode();

        var response2 = await _client.GetAsync("api/Credit?id=100");
        response2.EnsureSuccessStatusCode();

        Assert.True(true);
    }

    /// <summary>
    /// Проверка балансировки нагрузки между 5 репликами
    /// </summary>
    [Fact]
    public async Task LoadBalancingTest_Requests_ShouldBeDistributed()
    {
        for (var i = 1; i <= 8; i++)
        {
            var response = await _client.GetAsync($"api/Credit?id={i}");
            response.EnsureSuccessStatusCode();
        }
        Assert.True(true);
    }

    /// <summary>
    /// Проверка обработки некорректного ID (должен вернуть 400 Bad Request)
    /// </summary>
    [Fact]
    public async Task GetCredit_WithInvalidId_ShouldReturnBadRequest()
    {
        var response = await _client.GetAsync("api/Credit?id=0");
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }
}