using System.Text.Json;
using Microsoft.Extensions.Logging;
using Domain.Contracts;
using Domain.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

/// <summary>
/// Сервис для получения данных 
/// </summary>
public class VehicleContractCachedService(
        IVehicleContractGenerator generator,
        IDistributedCache cache,
        ILogger<VehicleContractCachedService> logger,
        IConfiguration configuration) : IVehicleContractCachedService
{
    private readonly int _cacheExpirationMinutes = configuration.GetValue<int>(
            "CacheSettings:VehicleContractExpirationMinutes");
    /// <summary>
    /// Функция для получения данных либо через Redis если там есть запись, либо генерация нового объекта
    /// </summary>
    public async Task<VehicleContractDto> GetVehicleContractAsync(int id)
    {
        var cacheKey = $"vehicle_contract_{id}";

        logger.LogInformation("Cache search started. CacheKey: {CacheKey}", cacheKey);
        var cachedValue = await cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrWhiteSpace(cachedValue))
        {
            logger.LogInformation("Cache found. CacheKey: {CacheKey}", cachedValue);
            var cachedContract = JsonSerializer.Deserialize<VehicleContractDto>(cachedValue);

            if (cachedContract is not null)
                return cachedContract;
        }
        logger.LogWarning("Cache not found. CacheKey: {CacheKey}", cacheKey);

        var contract = generator.Generate(id);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheExpirationMinutes)
        };

        var json = JsonSerializer.Serialize(contract);

        await cache.SetStringAsync(cacheKey, json, options);
        logger.LogWarning("Entry added to cache. CacheKey: {CacheKey}", cacheKey);
        return contract;
    }
}