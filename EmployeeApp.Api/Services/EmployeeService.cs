using EmployeeApp.Api.Entities;
using EmployeeApp.Api.Messaging;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace EmployeeApp.Api.Services;

/// <summary>
/// Реализация сервиса генерации данных сотрудников с кешированием и публикацией в брокер
/// </summary>
public class EmployeeService(IDistributedCache cache,
    IProducerService producer,
    ILogger<EmployeeService> logger,
    IConfiguration configuration) : IEmployeeService
{
    private readonly DistributedCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(configuration.GetValue("CacheLifetimeMinutes", 5))
    };

    /// <inheritdoc/>
    public async Task<Employee> GetEmployeeById(int id)
    {
        logger.LogInformation("Requesting employee with Id {Id}", id);

        var cached = await GetFromCache(id);
        if (cached is not null)
            return cached;

        logger.LogInformation("Cache miss for employee {Id}", id);

        var employee = EmployeeGenerator.Generate(id);

        await producer.SendMessage(employee);
        await SetToCache(id, employee);

        return employee;
    }

    /// <summary>
    /// Получение сотрудника из кэша по идентификатору
    /// </summary>
    private async Task<Employee?> GetFromCache(int id)
    {
        try
        {
            var cached = await cache.GetStringAsync($"employee:{id}");
            if (cached is null)
                return null;

            var employee = JsonSerializer.Deserialize<Employee>(cached);
            if (employee is not null)
                logger.LogInformation("Cache hit for employee {Id}", id);

            return employee;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading cache for employee {Id}", id);
            return null;
        }
    }

    /// <summary>
    /// Сохранение сотрудника в кэш
    /// </summary>
    private async Task SetToCache(int id, Employee employee)
    {
        try
        {
            await cache.SetStringAsync($"employee:{id}", JsonSerializer.Serialize(employee), _cacheOptions);
            logger.LogInformation("Generated and cached employee {Id}", id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error writing cache for employee {Id}", id);
        }
    }
}
