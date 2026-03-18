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
public class VehicleContractCachedService : IVehicleContractCachedService
{
    private readonly IVehicleContractGenerator _generator;
    private readonly IDistributedCache _cache;
    private ILogger<VehicleContractCachedService> _logger;
    private readonly int _cacheExpirationMinutes;
    /// <summary>
    /// Функция для получения данных либо через Redis если там есть запись, либо генерация нового объекта
    /// </summary>
    public VehicleContractCachedService(
        IVehicleContractGenerator generator,
        IDistributedCache cache,
        ILogger<VehicleContractCachedService> logger,
        IConfiguration configuration)
    {
        _generator = generator;
        _cache = cache;
        _logger = logger;
        _cacheExpirationMinutes = configuration.GetValue<int>(
            "CacheSettings:VehicleContractExpirationMinutes");
    }


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
        VehicleContractValidator.Validate(contract);

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
