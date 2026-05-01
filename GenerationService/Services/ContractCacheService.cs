using System.Text.Json;
using GenerationService.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace GenerationService.Services;

/// <summary>
/// Сервис кэширования контрактов через Redis
/// </summary>
public class ContractCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ContractGeneratorService _generator;
    private readonly ILogger<ContractCacheService> _logger;

    private static readonly DistributedCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public ContractCacheService(
        IDistributedCache cache,
        ContractGeneratorService generator,
        ILogger<ContractCacheService> logger)
    {
        _cache = cache;
        _generator = generator;
        _logger = logger;
    }

    public async Task<SoftwareProjectContract> GetOrCreateAsync(int id)
    {
        var cacheKey = $"contract:{id}";

        try
        {
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached is not null)
            {
                _logger.LogInformation("Cache HIT для ключа {CacheKey}", cacheKey);
                return JsonSerializer.Deserialize<SoftwareProjectContract>(cached)!;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка чтения из кэша для ключа {CacheKey}", cacheKey);
        }

        _logger.LogInformation("Cache MISS для ключа {CacheKey}. Генерация...", cacheKey);
        var contract = _generator.Generate(id);

        try
        {
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(contract),
                _cacheOptions);
            _logger.LogInformation("Контракт {Id} сохранён в кэш", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка записи в кэш для ключа {CacheKey}", cacheKey);
        }

        return contract;
    }
}