using ContractGenerator.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace CloudDevelopment.AppHost.Tests;

/// <summary>
/// Интеграционные тесты микросервисного сценария генерации и сохранения сотрудников.
/// </summary>
/// <param name="fixture">Фикстура с запущенным Aspire-окружением.</param>
public class IntegrationTests(AppHostFixture fixture) : IClassFixture<AppHostFixture>
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Проверяет, что API Gateway возвращает сгенерированного сотрудника с запрошенным идентификатором.
    /// </summary>
    [Fact]
    public async Task Gateway_ReturnsGeneratedEmployee()
    {
        var id = Random.Shared.Next(10_000, 20_000);

        using var response = await fixture.GatewayClient.GetAsync($"/employee?id={id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var employee = await response.Content.ReadFromJsonAsync<Employee>(_jsonOptions);

        Assert.NotNull(employee);
        Assert.Equal(id, employee.Id);
        Assert.False(string.IsNullOrWhiteSpace(employee.FullName));
        Assert.False(string.IsNullOrWhiteSpace(employee.Email));
    }

    /// <summary>
    /// Проверяет, что запрос через Gateway приводит к сохранению JSON-файла сотрудника в S3.
    /// </summary>
    [Fact]
    public async Task GatewayRequest_StoresEmployeeFileInS3()
    {
        var id = Random.Shared.Next(20_000, 30_000);

        var gatewayEmployee = await RequestEmployeeFromGateway(id);
        var storedEmployee = await WaitForStoredEmployee(id);

        Assert.Equivalent(gatewayEmployee, storedEmployee);
    }

    /// <summary>
    /// Проверяет, что повторный запрос возвращает кэшированные данные и не создает дубликаты файлов.
    /// </summary>
    [Fact]
    public async Task RepeatedGatewayRequest_ReturnsCachedEmployee_AndDoesNotCreateDuplicateFiles()
    {
        var id = Random.Shared.Next(30_000, 40_000);
        var expectedKey = $"employee_{id}.json";

        var firstEmployee = await RequestEmployeeFromGateway(id);
        var secondEmployee = await RequestEmployeeFromGateway(id);
        _ = await WaitForStoredEmployee(id);

        using var listResponse = await fixture.FileServiceClient.GetAsync("/api/files");
        listResponse.EnsureSuccessStatusCode();
        var keys = await listResponse.Content.ReadFromJsonAsync<List<string>>(_jsonOptions);

        Assert.NotNull(keys);
        Assert.Equivalent(firstEmployee, secondEmployee);
        Assert.Single(keys, key => key == expectedKey);
    }

    /// <summary>
    /// Проверяет, что серия запросов через Gateway сохраняет все ожидаемые файлы сотрудников в S3.
    /// </summary>
    [Fact]
    public async Task MultipleGatewayRequests_StoreAllEmployeesInS3()
    {
        var startId = Random.Shared.Next(40_000, 50_000);
        var ids = Enumerable.Range(startId, 10).ToArray();
        var expectedKeys = ids.Select(id => $"employee_{id}.json").ToArray();

        foreach (var id in ids)
        {
            _ = await RequestEmployeeFromGateway(id);
        }

        var keys = await WaitForStoredKeys(expectedKeys);

        foreach (var expectedKey in expectedKeys)
        {
            Assert.Contains(expectedKey, keys);
        }
    }

    /// <summary>
    /// Запрашивает сотрудника через Gateway и проверяет базовую корректность ответа.
    /// </summary>
    /// <param name="id">Идентификатор сотрудника.</param>
    private async Task<Employee> RequestEmployeeFromGateway(int id)
    {
        using var response = await fixture.GatewayClient.GetAsync($"/employee?id={id}");
        response.EnsureSuccessStatusCode();

        var employee = await response.Content.ReadFromJsonAsync<Employee>(_jsonOptions);

        Assert.NotNull(employee);
        Assert.Equal(id, employee.Id);

        return employee;
    }

    /// <summary>
    /// Ожидает появления файла сотрудника в S3 и возвращает его содержимое.
    /// </summary>
    /// <param name="id">Идентификатор сотрудника.</param>
    private async Task<Employee> WaitForStoredEmployee(int id)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (DateTime.UtcNow < deadline)
        {
            using var response = await fixture.FileServiceClient.GetAsync($"/api/files/{id}");
            if (response.IsSuccessStatusCode)
            {
                var employee = await response.Content.ReadFromJsonAsync<Employee>(_jsonOptions);
                Assert.NotNull(employee);
                Assert.Equal(id, employee.Id);
                return employee;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException($"Employee file employee_{id}.json was not stored in S3 in time");
    }

    /// <summary>
    /// Ожидает появления всех указанных ключей файлов в S3.
    /// </summary>
    /// <param name="expectedKeys">Ожидаемые ключи файлов.</param>
    private async Task<IReadOnlyList<string>> WaitForStoredKeys(IReadOnlyCollection<string> expectedKeys)
    {
        var deadline = DateTime.UtcNow.AddSeconds(45);

        while (DateTime.UtcNow < deadline)
        {
            using var response = await fixture.FileServiceClient.GetAsync("/api/files");
            response.EnsureSuccessStatusCode();

            var keys = await response.Content.ReadFromJsonAsync<List<string>>(_jsonOptions) ?? [];
            if (expectedKeys.All(keys.Contains))
            {
                return keys;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException("Expected employee files were not stored in S3 in time");
    }
}
