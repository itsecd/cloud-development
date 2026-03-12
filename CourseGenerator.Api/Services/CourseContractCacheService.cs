using System.Text.Json;
using CourseGenerator.Api.Interfaces;
using CourseGenerator.Api.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace CourseGenerator.Api.Services;

/// <summary>
/// Сервис работы с Redis-кэшем для списков учебных контрактов.
/// </summary>
public sealed class CourseContractCacheService(IDistributedCache cache, ILogger<CourseContractCacheService> logger) : ICourseContractCacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CourseContract>?> GetAsync(int count, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(count);
        var cachedPayload = await cache.GetStringAsync(key, cancellationToken);

        if (string.IsNullOrWhiteSpace(cachedPayload))
        {
            logger.LogInformation("Cache miss for key {CacheKey}", key);
            return null;
        }

        logger.LogInformation("Cache hit for key {CacheKey}", key);

        return JsonSerializer.Deserialize<IReadOnlyList<CourseContract>>(cachedPayload, SerializerOptions);
    }

    /// <inheritdoc />
    public async Task SetAsync(int count, IReadOnlyList<CourseContract> contracts, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(count);
        var payload = JsonSerializer.Serialize(contracts, SerializerOptions);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        await cache.SetStringAsync(key, payload, options, cancellationToken);
        logger.LogInformation("Cache updated for key {CacheKey}", key);
    }

    private static string BuildKey(int count) => $"courses:count:{count}";
}
