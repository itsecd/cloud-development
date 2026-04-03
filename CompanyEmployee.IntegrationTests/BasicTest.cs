using CompanyEmployee.Domain.Entity;
using System.Text.Json;

namespace CompanyEmployee.IntegrationTests;

/// <summary>
/// Базовые интеграционные тесты.
/// </summary>
public class BasicTests
{
    private readonly HttpClient _client;

    public BasicTests()
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:6001")
        };
    }

    /// <summary>
    /// Проверяет, что API генерирует сотрудника.
    /// </summary>
    [Fact]
    public async Task GetEmployee_ShouldReturnEmployee()
    {
        var response = await _client.GetAsync("/api/employee/1");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var employee = JsonSerializer.Deserialize<Employee>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(employee);
        Assert.Equal(1, employee.Id);
        Assert.False(string.IsNullOrEmpty(employee.FullName));
    }

    /// <summary>
    /// Проверяет работу кэширования.
    /// </summary>
    [Fact]
    public async Task SameEmployeeId_ShouldReturnSameData()
    {
        var response1 = await _client.GetAsync("/api/employee/2");
        var content1 = await response1.Content.ReadAsStringAsync();

        var response2 = await _client.GetAsync("/api/employee/2");
        var content2 = await response2.Content.ReadAsStringAsync();

        Assert.Equal(content1, content2);
    }

    /// <summary>
    /// Проверяет, что разные ID генерируют разных сотрудников.
    /// </summary>
    [Fact]
    public async Task DifferentIds_ShouldGenerateDifferentEmployees()
    {
        var response1 = await _client.GetAsync("/api/employee/10");
        var employee1 = JsonSerializer.Deserialize<Employee>(
            await response1.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var response2 = await _client.GetAsync("/api/employee/20");
        var employee2 = JsonSerializer.Deserialize<Employee>(
            await response2.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(employee1);
        Assert.NotNull(employee2);
        Assert.NotEqual(employee1.FullName, employee2.FullName);
    }
}