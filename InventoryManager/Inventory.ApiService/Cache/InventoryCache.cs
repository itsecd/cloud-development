using System.Text.Json;
using Inventory.ApiService.Entity;
using Inventory.ApiService.Generation;
using Microsoft.Extensions.Caching.Distributed;

namespace Inventory.ApiService.Cache;
/// <summary>
/// Реализация сервиса кэширования для получения продукта.
/// Сначала пытается получить данные из кэша, при отсутствии — генерирует продукт и сохраняет его в кэш.
/// </summary>
/// <param name="cache"> Сервис распределённого кэширования</param>
/// <param name="configuration"> Конфигурация приложения</param>
/// <param name="logger"> Логгер для записи событий</param>
/// <param name="generator"> Генератор </param>
public class InventoryCache(IDistributedCache cache, IConfiguration configuration, ILogger<InventoryCache> logger,Generator generator) : IInventoryCache
{
    /// <summary>
    /// Возвращает продукт по идентификатору.
    /// При наличии в кэше возвращает сохранённые данные, иначе генерирует новый объект и сохраняет его в кэш 
    /// </summary>
    /// <param name="id"> Идентификатор продукта</param>
    /// <param name="ct"> Токен отмены операции</param>
    /// <returns></returns>
    public async Task<Product> GetAsync(int id, CancellationToken ct)
    {
        var cacheKey = $"inventory-{id}";
        logger.LogInformation("Try get product {Id} from cache", id);

        string? cachedData = null;

        try
        {
            cachedData = await cache.GetStringAsync(cacheKey, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache READ failed for {Id}. Continue without cache.", id);
        }

        if (!string.IsNullOrEmpty(cachedData))
        {
            try
            {
                var cachedProduct = JsonSerializer.Deserialize<Product>(cachedData);
                if (cachedProduct is not null)
                {
                    logger.LogInformation("Cache HIT for product {Id}", id);
                    return cachedProduct;
                }

                logger.LogWarning("Cache HIT but deserialize returned null for product {Id}", id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Deserialize failed for product {Id}. Continue without cache.", id);
            }
        }

        logger.LogInformation("Cache MISS for product {Id}. Generating.", id);
        var product = generator.Generate(id);

        try
        {
            var expirationMinutes = configuration.GetValue("CacheSettings:ExpirationMinutes", 5);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expirationMinutes)
            };

            await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(product), options, ct);
            logger.LogInformation("Product {Id} saved to cache", id);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache WRITE failed for {Id}. Continue without cache.", id);
        }

        return product;
    }
}