using Generator.Dto;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Generator.Services;

/// <summary>
/// Сервис получения кредитной заявки по идентификатору.
/// Сначала пытается вернуть данные из распределённого кэша, при отсутствии данных в кэше — генерирует заявку и кэширует результат.
/// </summary>
public class CreditOrderService(
    IDistributedCache cache,
    CreditOrderGenerator generator,
    IConfiguration cfg,
    ILogger<CreditOrderService> logger
    )
{

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Возвращает заявку по <paramref name="id"/>:
    /// 1) читает из кэша по ключу <c>credit-order:{id}</c>;
    /// 2) при отсутствии данных в кэше генерирует через <see cref="CreditOrderGenerator"/>;
    /// 3) сохраняет в кэш с TTL (AbsoluteExpirationRelativeToNow).
    /// </summary>
    /// <param name="id">Идентификатор заявки (должен быть больше 0).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>DTO заявки.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Если <paramref name="id"/> &lt;= 0.</exception>
    /// <exception cref="OperationCanceledException">Если запрос был отменён.</exception>
    public async Task<CreditOrderDto> GetByIdAsync(int id, CancellationToken ct)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id), "invalid id");

        var ttlSeconds = cfg.GetValue("CreditOrderCache:TtlSeconds", 300);
        if (ttlSeconds <= 0) ttlSeconds = 300;

        var cacheKey = BuildCacheKey(id);

        try
        {
            var cachedJson = await cache.GetStringAsync(cacheKey, ct);
            if (!string.IsNullOrWhiteSpace(cachedJson))
            {
                var cached = JsonSerializer.Deserialize<CreditOrderDto>(cachedJson, _jsonOptions);
                if (cached is not null)
                {
                    logger.LogInformation("Cache HIT: {CacheKey} {OrderId}", cacheKey, id);
                    return cached;
                }

                logger.LogWarning("Cache DESERIALIZE FAIL: {CacheKey} {OrderId}", cacheKey, id);
            }
            else
            {
                logger.LogInformation("Cache MISS: {CacheKey} {OrderId}", cacheKey, id);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Request canceled: {CacheKey} {OrderId}", cacheKey, id);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache READ FAIL: {CacheKey} {OrderId}", cacheKey, id);
        }

        var order = generator.Generate(id);

        try 
        {
            var cacheTtl = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttlSeconds)
            };
            var json = JsonSerializer.Serialize(order, _jsonOptions);
            await cache.SetStringAsync(cacheKey, json, cacheTtl, ct);
            logger.LogInformation("Cache SET: {CacheKey} {OrderId}", cacheKey, id);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Request canceled: {CacheKey} {OrderId}", cacheKey, id);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache WRITE FAIL: {CacheKey} {OrderId}", cacheKey, id);
        }
        return order;
    }

    /// <summary>
    /// Формирует ключ кэша для заявки по идентификатору.
    /// Формат: <c>credit-order:{id}</c>.
    /// </summary>
    private static string BuildCacheKey(int id) => $"credit-order:{id}";
}
