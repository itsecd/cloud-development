using System.Text.Json;
using Microsoft.Extensions.Logging;
using Domain.Contracts;
using Domain.Interfaces;
using Microsoft.Extensions.Caching.Distributed;


namespace Infrastructure.Services;

/// <summary>
/// Сервис для получения данных 
/// </summary>
public class VehicleContractCachedService : IVehicleContractCachedService
{
    private readonly IVehicleContractGenerator _generator;
    private readonly IDistributedCache _cache;
    private ILogger<VehicleContractCachedService> _logger;
    /// <summary>
    /// Функция для получения данных либо через Redis если там есть запись, либо генерация нового объекта
    /// </summary>
    public VehicleContractCachedService(
        IVehicleContractGenerator generator,
        IDistributedCache cache,
        ILogger<VehicleContractCachedService> logger)
    {
        _generator = generator;
        _cache = cache;
        _logger = logger;
    }


    public async Task<VehicleContractDto> GetVehicleContractAsync(int seed)
    {
        var cacheKey = $"vehicle_contract_{seed}";

        _logger.LogInformation("Cache search started. CacheKey: {CacheKey}", cacheKey);
        var cachedValue = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrWhiteSpace(cachedValue))
        {
            _logger.LogInformation("Cache found. CacheKey: {CacheKey}", cachedValue);
            var cachedContract = JsonSerializer.Deserialize<VehicleContractDto>(cachedValue);

            if (cachedContract is not null)
                return cachedContract;
        }
        _logger.LogWarning("Cache not found. CacheKey: {CacheKey}", cacheKey);

        var contract = _generator.Generate(seed);
        VehicleContractValidator.Validate(contract);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        var json = JsonSerializer.Serialize(contract);

        await _cache.SetStringAsync(cacheKey, json, options);
        _logger.LogWarning("Entry added to cache. CacheKey: {CacheKey}", cacheKey);
        return contract;
    }
}
