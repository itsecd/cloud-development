using System.Text.Json;
using GenerationService.Models;
using GenerationService.Options;
using GenerationService.Services;    
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace GenerationService.Services;

/// <summary>
/// Сервис кэширования контрактов через Redis
/// </summary>
public class ContractCacheService(
    IDistributedCache cache,
    ContractGeneratorService generator,
    SnsPublisherService snsPublisher,   
    ILogger<ContractCacheService> logger,
    IOptions<CacheOptions> options)
{
    private readonly DistributedCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow =
            TimeSpan.FromMinutes(options.Value.ExpirationMinutes)
    };

    public async Task<SoftwareProjectContract> GetOrCreateAsync(int id)
    {
        var cacheKey = $"contract:{id}";

        try
        {
            var cached = await cache.GetStringAsync(cacheKey);

            if (cached is not null)
            {
                logger.LogInformation("Cache HIT для ключа {CacheKey}", cacheKey);
                return JsonSerializer.Deserialize<SoftwareProjectContract>(cached)!;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка чтения из кэша для ключа {CacheKey}", cacheKey);
        }

        logger.LogInformation("Cache MISS для ключа {CacheKey}. Генерация...", cacheKey);

        var contract = generator.Generate(id);

        try
        {
            await cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(contract),
                _cacheOptions);

            logger.LogInformation("Контракт {Id} сохранён в кэш", id);

            // === Публикация в SNS после успешного кэширования ===
            await snsPublisher.PublishAsync(contract);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка записи в кэш или публикации в SNS для ключа {CacheKey}", cacheKey);
        }

        return contract;
    }
}