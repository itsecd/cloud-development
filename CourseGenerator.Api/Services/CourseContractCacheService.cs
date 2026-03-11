using System.Text.Json;
using CourseGenerator.Api.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace CourseGenerator.Api.Services;

public interface ICourseContractCacheService
{
    Task<IReadOnlyList<CourseContract>?> GetAsync(int count, CancellationToken cancellationToken = default);
    Task SetAsync(int count, IReadOnlyList<CourseContract> contracts, CancellationToken cancellationToken = default);
}

public sealed class CourseContractCacheService(IDistributedCache cache) : ICourseContractCacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<CourseContract>?> GetAsync(int count, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(count);
        var cachedPayload = await cache.GetStringAsync(key, cancellationToken);

        if (string.IsNullOrWhiteSpace(cachedPayload))
        {
            return null;
        }

        return JsonSerializer.Deserialize<IReadOnlyList<CourseContract>>(cachedPayload, SerializerOptions);
    }

    public async Task SetAsync(int count, IReadOnlyList<CourseContract> contracts, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(count);
        var payload = JsonSerializer.Serialize(contracts, SerializerOptions);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        await cache.SetStringAsync(key, payload, options, cancellationToken);
    }

    private static string BuildKey(int count) => $"courses:count:{count}";
}
