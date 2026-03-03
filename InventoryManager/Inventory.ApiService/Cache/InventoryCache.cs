using System.Text.Json;
using Inventory.ApiService.Entity;
using Inventory.ApiService.Generation;
using Microsoft.Extensions.Caching.Distributed;

namespace Inventory.ApiService.Cache;
/// <summary>
/// Реализация сервиса кэширования для получения продукта.
/// Сначала пытается получить данные из кэша, при отсутствии — генерирует продукт и сохраняет его в кэш.
/// </summary>
/// <param name="_cache"> Сервис распределённого кэширования</param>
/// <param name="_configuration"> Конфигурация приложения</param>
/// <param name="_logger"> Логгер для записи событий</param>
/// <param name="_generator"> Генератор </param>
public class InventoryCache(IDistributedCache cache, IConfiguration configuration, ILogger<InventoryCache> logger,Generator generator) : IInventoryCache
{
    private readonly IDistributedCache _cache = cache;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<InventoryCache> _logger = logger;
    private readonly Generator _generator = generator;

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
        _logger.LogInformation("Try get product {Id} from cache", id);

        string? cachedData = null;

        try
        {
            cachedData = await _cache.GetStringAsync(cacheKey, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache READ failed for {Id}. Continue without cache.", id);
        }

        if (!string.IsNullOrEmpty(cachedData))
        {
            try
            {
                var cachedProduct = JsonSerializer.Deserialize<Product>(cachedData);
                if (cachedProduct is not null)
                {
                    _logger.LogInformation("Cache HIT for product {Id}", id);
                    return cachedProduct;
                }

                _logger.LogWarning("Cache HIT but deserialize returned null for product {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Deserialize failed for product {Id}. Continue without cache.", id);
            }
        }

        _logger.LogInformation("Cache MISS for product {Id}. Generating.", id);
        var product = _generator.Generate(id);

        try
        {
            var expirationMinutes = _configuration.GetValue("CacheSettings:ExpirationMinutes", 5);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expirationMinutes)
            };

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(product), options, ct);
            _logger.LogInformation("Product {Id} saved to cache", id);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache WRITE failed for {Id}. Continue without cache.", id);
        }

        return product;
    }
}