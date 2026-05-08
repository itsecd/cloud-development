using Cloud.Api.Messaging;
using Cloud.Api.Models;
using Cloud.Api.Services;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Cloud.Api.Services;

/// <summary>
/// Сервис для получения сотрудника компании по id с кэшированием
/// </summary>
/// <param name="generator">Генератор сотрудника</param>
/// <param name="cache">Сервис кэширования</param>
/// <param name="configuration">Конфигурация приложения</param>
/// <param name="logger">Логгер</param>
public class EmployeeService(
    IEmployeeGenerator generator,
    IProducerService producer,
    IDistributedCache cache,
    IConfiguration configuration,
    ILogger<EmployeeService> logger) : IEmployeeService
{
    private readonly string _cacheKeyPrefix = configuration.GetValue("CacheKeyPrefix", "employee");
    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(configuration.GetValue("CacheTtlMinutes", 30));

    /// <inheritdoc />
    public async Task<Employee> GetOrGenerateAsync(int id)
    {
        var cacheKey = $"{_cacheKeyPrefix}:{id}";

        var cached = await GetFromCache(cacheKey);
        if (cached is not null)
        {
            return cached;
        }

        logger.LogInformation("Cache miss for employee {Id}, generating new data", id);
        var employee = generator.Generate(id);
        await producer.SendMessage(employee);
        await SetToCache(cacheKey, employee);
        return employee;
    }

    /// <summary>
    /// Метод получения сотрудника из кэша
    /// </summary>
    /// <param name="cacheKey">Ключ кэша</param>
    /// <returns> 
    /// Сотрудник, или null в случае ошибки или отсутствия данных в кэше
    /// </returns>
    private async Task<Employee?> GetFromCache(string cacheKey)
    {
        try
        {
            var cached = await cache.GetStringAsync(cacheKey);
            if (cached is null) return null;

            logger.LogInformation("Cache hit for key {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<Employee>(cached);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read from cache for key {CacheKey}", cacheKey);
            return null;
        }
    }

    /// <summary>
    /// Сохранение сотрудника в кэш с обработкой ошибок при записи
    /// </summary>
    /// <param name="cacheKey">Ключ кэша</param>
    /// <param name="employee">Сотрудник компании</param>
    private async Task SetToCache(string cacheKey, Employee employee)
    {
        try
        {
            var json = JsonSerializer.Serialize(employee);
            await cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheTtl
            });
            logger.LogInformation("Cached employee {Id} with key {CacheKey}", employee.Id, cacheKey);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write to cache for key {CacheKey}", cacheKey);
        }
    }
}