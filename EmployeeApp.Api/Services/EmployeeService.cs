using System.Text.Json;
using EmployeeApp.Api.Entities;
using Microsoft.Extensions.Caching.Distributed;

namespace EmployeeApp.Api.Services;

/// <summary>
/// Реализация сервиса генерации данных сотрудников с кешированием
/// </summary>
public class EmployeeService(IDistributedCache cache, ILogger<EmployeeService> logger, IConfiguration configuration) : IEmployeeService
{
    private readonly DistributedCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(configuration.GetValue("CacheLifetimeMinutes", 5))
    };

    /// <inheritdoc/>
    public async Task<Employee> GetEmployeeById(int id)
    {
        var cacheKey = $"employee:{id}";

        logger.LogInformation("Requesting employee with Id {Id}", id);

        try
        {
            var cached = await cache.GetStringAsync(cacheKey);
            if (cached is not null)
            {
                var cachedEmployee = JsonSerializer.Deserialize<Employee>(cached);
                if (cachedEmployee is not null)
                {
                    logger.LogInformation("Cache hit for employee {Id}", id);
                    return cachedEmployee;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading cache for employee {Id}", id);
        }

        logger.LogInformation("Cache miss for employee {Id}", id);

        var employee = EmployeeGenerator.Generate(id);

        try
        {
            await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(employee), _cacheOptions);
            logger.LogInformation("Generated and cached employee {Id}", id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error writing cache for employee {Id}", id);
        }

        return employee;
    }
}
