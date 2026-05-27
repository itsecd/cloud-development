using System.Text.Json;
using ContractGenerator.Api.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace ContractGenerator.Api.Services;

/// <summary>
/// Реализация получения сотрудника компании с использованием Redis-кэша.
/// </summary>
/// <param name="generator">Генератор сотрудника.</param>
/// <param name="publisher">Публикатор новых сотрудников в брокер сообщений.</param>
/// <param name="cache">Сервис кэширования.</param>
/// <param name="configuration">Конфигурация приложения.</param>
/// <param name="logger">Логгер.</param>
public class CachedEmployeeService(
    IEmployeeGenerator generator,
    IEmployeePublisher publisher,
    IDistributedCache cache,
    IConfiguration configuration,
    ILogger<CachedEmployeeService> logger) : IEmployeeService
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

        logger.LogInformation("Cache miss for employee {EmployeeId}, generating new data", id);
        var employee = generator.Generate(id);
        await SetToCache(cacheKey, employee);
        await publisher.PublishAsync(employee);
        return employee;
    }

    /// <summary>
    /// Пытается прочитать сотрудника из кэша.
    /// </summary>
    /// <param name="cacheKey">Ключ кэша.</param>
    /// <returns>Сотрудник или null, если данных нет либо произошла ошибка чтения.</returns>
    private async Task<Employee?> GetFromCache(string cacheKey)
    {
        try
        {
            var cached = await cache.GetStringAsync(cacheKey);
            if (cached is null)
            {
                return null;
            }

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
    /// Записывает данные сотрудника в кэш.
    /// </summary>
    /// <param name="cacheKey">Ключ кэша.</param>
    /// <param name="employee">Сотрудник компании.</param>
    private async Task SetToCache(string cacheKey, Employee employee)
    {
        try
        {
            var json = JsonSerializer.Serialize(employee);
            await cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheTtl
            });
            logger.LogInformation("Cached employee {EmployeeId} with key {CacheKey}", employee.Id, cacheKey);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write to cache for key {CacheKey}", cacheKey);
        }
    }
}
