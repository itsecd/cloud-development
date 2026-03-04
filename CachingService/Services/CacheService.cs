using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Domain.Entities;

namespace CachingService.Services;

public class CacheService(IDistributedCache cache, IConfiguration configuration) : ICacheService
{
    private readonly TimeSpan _cacheExpiration =  int.TryParse(configuration["CacheTime"], out var sec)
        ? TimeSpan.FromSeconds(sec)
        : TimeSpan.FromSeconds(3600);

    public async Task<MedicalPatient?> RetriveFromCache(int id)
    {
        var cachedData = await cache.GetStringAsync(id.ToString());
        if (!string.IsNullOrEmpty(cachedData))
        {
            return JsonSerializer.Deserialize<MedicalPatient>(cachedData);
        }

        return null;
    }

    public async Task PutInCache(MedicalPatient patient)
    {
        var data = JsonSerializer.Serialize(patient);
        await cache.SetStringAsync(patient.Id.ToString(), data,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration
            });
    }
}