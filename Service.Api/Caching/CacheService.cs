using Microsoft.Extensions.Caching.Distributed;
using Service.Api.Entities;
using System.Text.Json;

namespace Service.Api.Caching;

/// <summary>
/// Служба для с кэшем сотрудников компании
/// </summary>
/// <param name="cache">Кэш</param>
/// <param name="logger">Логгер</param>
public class CacheService(IDistributedCache cache, ILogger<CacheService> logger) : ICacheService
{
    /// <summary>
    /// Время жизни кэша
    /// </summary>
    private static readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    /// <inheritdoc/>
    public async Task<Employee?> RetrieveFromCache(int id)
    {
        try
        {
            var json = await cache.GetStringAsync(id.ToString());
            if (string.IsNullOrEmpty(json))
                return null;
            return JsonSerializer.Deserialize<Employee>(json);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving employee {EmployeeId} from cache", id);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task PopulateCache(Employee employee)
    {
        try
        {
            var json = JsonSerializer.Serialize(employee);
            await cache.SetStringAsync(employee.Id.ToString(), json,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheExpiration
                });
            logger.LogDebug("Successfully cached employee {EmployeeId}", employee.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to cache employee {EmployeeId}", employee.Id);
        }
    }
}
