using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using PatientApp.Generator.Services;
using PatientApp.Generator.Models;

public class PatientService(
    PatientGenerator generator,
    IDistributedCache cache,
    ILogger<PatientService> logger,
    IConfiguration config
)
{
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(config.GetSection("CacheSetting").GetValue("CacheExpirationMinutes", 5));
    private const string CacheKeyPrefix = "patient:";

    public async Task<Patient> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";

        logger.LogInformation("Patient with Id: {id} was requested", id);

        var cachedData = await cache.GetStringAsync(cacheKey, cancellationToken);

        if (!string.IsNullOrEmpty(cachedData))
        {
            logger.LogInformation("Patient with {id} was found in cache", id);
            var cachedPatient = JsonSerializer.Deserialize<Patient>(cachedData);
            if (cachedPatient != null) return cachedPatient;
        }

        logger.LogInformation("Patient with {id} was found in cache, start generating", id);

        var patient = generator.Generate(id);

        var serializedData = JsonSerializer.Serialize(patient);
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration
        };

        await cache.SetStringAsync(cacheKey, serializedData, cacheOptions, cancellationToken);

        logger.LogInformation("Patint with Id: {id} was saved to cache with TTL {TtlMinutes} minutes", id, _cacheExpiration.TotalMinutes);

        return patient;
    }
}