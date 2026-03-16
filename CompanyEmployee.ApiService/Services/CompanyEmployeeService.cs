using System.Text.Json;
using CompanyEmployee.ApiService.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;

namespace CompanyEmployee.ApiService.Services;

/// <summary>
/// Сервис для получения данных сотрудника
/// </summary>
/// <param name="cache">кэш Redis</param>
/// <param name="logger">логгер</param>
/// <param name="configuration">конфигурация</param>
public class CompanyEmployeeService(IDistributedCache cache, ILogger<CompanyEmployeeService> logger, IConfiguration configuration)
{
    private readonly int _cacheTime = configuration.GetValue<int>("Constants:CacheTime");

    /// <summary>
    /// Получение данных сотрудника по его id, при отсутствии генерация сотрудника 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<CompanyEmployeeModel> GetEmployeeAsync(int id)
    {
        var cacheKey = $"employee:{id}";

        var cachedEmployee = await cache.GetStringAsync(cacheKey);

        if (cachedEmployee != null)
        {
            var employeeModel = JsonSerializer.Deserialize<CompanyEmployeeModel>(cachedEmployee);

            if (employeeModel != null)
            {
                logger.LogInformation("Сотрудник №{Id} получен из кэша", id);
                return employeeModel;
            }
        }

        logger.LogInformation("Генерация сотрудника №{Id}", id);

        var employee = CompanyEmployeeGenerator.Generate(id);

        await cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(employee),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheTime)
            });

        return employee;
    }
}