using Generator.Dto;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Generator.Services;

public sealed class CreditOrderService
{

    private static readonly DistributedCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
    };

    private readonly IDistributedCache _cache;
    private readonly CreditOrderGenerator _generator;
    private readonly ILogger<CreditOrderService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public CreditOrderService(
        IDistributedCache cache,
        CreditOrderGenerator generator,
        ILogger<CreditOrderService> logger)
    {
        _cache = cache;
        _generator = generator;
        _logger = logger;
    }
    public async Task<CreditOrderDto> GetByIdAsync(int id, CancellationToken ct)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id), "invalid id");

        var cacheKey = BuildCacheKey(id);

        try
        {
            var cachedJson = await _cache.GetStringAsync(cacheKey, ct);
            if (!string.IsNullOrWhiteSpace(cachedJson))
            {
                var cached = JsonSerializer.Deserialize<CreditOrderDto>(cachedJson, _jsonOptions);
                if (cached is not null)
                {
                    _logger.LogInformation("Cache HIT: {CacheKey} {OrderId}", cacheKey, id);
                    return cached;
                }

                _logger.LogWarning("Cache DESERIALIZE FAIL: {CacheKey} {OrderId}", cacheKey, id);
            }
            else
            {
                _logger.LogInformation("Cache MISS: {CacheKey} {OrderId}", cacheKey, id);
            }

            var order = _generator.Generate(id);

            var json = JsonSerializer.Serialize(order, _jsonOptions);
            await _cache.SetStringAsync(cacheKey, json, _cacheOptions, ct);

            _logger.LogInformation("Cache SET: {CacheKey} {OrderId}", cacheKey, id);

            return order;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request canceled: {CacheKey} {OrderId}", cacheKey, id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreditOrderService failed: {CacheKey} {OrderId}", cacheKey, id);
            throw;
        }

    }

    private static string BuildCacheKey(int id) => $"credit-order:{id}";
}
