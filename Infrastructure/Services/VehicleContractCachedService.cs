using System.Text.Json;
using Microsoft.Extensions.Logging;
using Domain.Contracts;
using Domain.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.Services;

public class VehicleContractCachedService : IVehicleContractCachedService
{
    private readonly IVehicleContractGenerator _generator;
    private readonly IDistributedCache _cache;
    private ILogger<VehicleContractCachedService> _logger;
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

        _logger.LogInformation("Начался поиск кэша. CacheKey: {CacheKey}", cacheKey);
        var cachedValue = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrWhiteSpace(cachedValue))
        {
            _logger.LogInformation("Кэш найден. CacheKey: {CacheKey}", cachedValue);
            var cachedContract = JsonSerializer.Deserialize<VehicleContractDto>(cachedValue);

            if (cachedContract is not null)
                return cachedContract;
        }
        _logger.LogWarning("Кэш не найден. CacheKey: {CacheKey}", cacheKey);

        var contract = _generator.Generate(seed);
        VehicleContractValidator.Validate(contract);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        var json = JsonSerializer.Serialize(contract);

        await _cache.SetStringAsync(cacheKey, json, options);
        _logger.LogWarning("Добавлена запись в кэш. CacheKey: {CacheKey}", cacheKey);
        return contract;
    }
}