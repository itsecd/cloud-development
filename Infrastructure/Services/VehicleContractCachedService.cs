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
    private readonly IVehicleContractGenerator _generator = generator;
    private readonly IDistributedCache _cache = cache;
    private ILogger<VehicleContractCachedService> _logger = logger;
    private readonly int _cacheExpirationMinutes = configuration.GetValue<int>(
            "CacheSettings:VehicleContractExpirationMinutes");
    /// <summary>
    /// Функция для получения данных либо через Redis если там есть запись, либо генерация нового объекта
    /// </summary>
    public async Task<VehicleContractDto> GetVehicleContractAsync(int id)
    {
        var cacheKey = $"vehicle_contract_{id}";

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

        var contract = _generator.Generate(id);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheExpirationMinutes)
        };

        var json = JsonSerializer.Serialize(contract);

        await _cache.SetStringAsync(cacheKey, json, options);
        _logger.LogWarning("Entry added to cache. CacheKey: {CacheKey}", cacheKey);
        return contract;
    }
}
