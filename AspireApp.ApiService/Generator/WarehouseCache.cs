using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using AspireApp.ApiService.Entities;

namespace AspireApp.ApiService.Generator;

/// <summary>
/// Кэширование товаров 
/// </summary>
public class WarehouseCache(
    IDistributedCache cache,
    ILogger<WarehouseCache> logger,
    IConfiguration configuration) : IWarehouseCache
{
    private readonly TimeSpan _defaultExpiration = int.TryParse(configuration["CacheExpiration"], out var seconds)
        ? TimeSpan.FromSeconds(seconds)
        : TimeSpan.FromSeconds(3600);

    public async Task<Warehouse?> GetAsync(int id)
    {
        var key = $"warehouse_{id}";
        var cached = await cache.GetStringAsync(key);
        if (cached == null)
            return null;

        try
        {
            return JsonSerializer.Deserialize<Warehouse>(cached);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка десериализации товара {Id} из кэша", id);
            return null;
        }
    }

    public async Task SetAsync(Warehouse warehouse)
    {
        var key = $"warehouse_{warehouse.Id}";
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _defaultExpiration
        };
        var serialized = JsonSerializer.Serialize(warehouse);
        await cache.SetStringAsync(key, serialized, options);
    }
}