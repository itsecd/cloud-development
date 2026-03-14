using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Patient.Generator.DTO;

namespace Patient.Generator.Service;

/// <summary>
/// Реализация кэширования медицинских пациентов с использованием распределенного кэша.
/// </summary>
public sealed class PatientCache(
    ILogger<PatientCache> logger,
    IDistributedCache cache,
    IConfiguration configuration) : IPatientCache
{
    private const string CacheKeyPrefix = "patient:";
    private const int CacheExpirationTimeMinutesDefault = 15;

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(
        configuration.GetValue("CacheSettings:ExpirationTimeMinutes", CacheExpirationTimeMinutesDefault));

    /// <summary>
    /// Получить пациента из кэша по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор пациента.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>DTO пациента или null, если не найден в кэше.</returns>
    public async Task<PatientDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";

        string? json;
        try
        {
            json = await cache.GetStringAsync(cacheKey, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache read failed for key={cacheKey}.", cacheKey);
            return null;
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            logger.LogInformation("Cache miss for key={cacheKey}.", cacheKey);
            return null;
        }

        try
        {
            var obj = JsonSerializer.Deserialize<PatientDto>(json, _jsonOptions);
            if (obj is null)
            {
                logger.LogWarning("Cache value for key={cacheKey} deserialized as null.", cacheKey);
                return null;
            }

            logger.LogInformation("Cache hit for id={id}.", obj.Id);
            return obj;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache JSON invalid for key={cacheKey}.", cacheKey);
            return null;
        }
    }

    /// <summary>
    /// Сохранить пациента в кэш.
    /// </summary>
    /// <param name="id">Идентификатор пациента.</param>
    /// <param name="value">DTO пациента для сохранения.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public async Task SetAsync(int id, PatientDto value, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";

        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheTtl
            };

            var json = JsonSerializer.Serialize(value, _jsonOptions);
            await cache.SetStringAsync(cacheKey, json, options, cancellationToken);
            logger.LogInformation("Cached id={id} for ttl={ttlMinutes}m.", value.Id, _cacheTtl.TotalMinutes);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache write failed for id={id}.", value.Id);
        }
    }
}
