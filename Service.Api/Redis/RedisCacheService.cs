using System.Text.Json;
using Service.Api.Entity;
using StackExchange.Redis;

namespace Service.Api.Redis;

public class RedisCacheService
{
    private readonly IDatabase _db;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        this._db = redis.GetDatabase();
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expire)
    {
        var json = JsonSerializer.Serialize(value);
        await this._db.StringSetAsync(key, json, expire);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await this._db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return default;
        return JsonSerializer.Deserialize<T>(value!);
    }

    public async Task RemoveAsync(string key)
    {
        await this._db.KeyDeleteAsync(key);
    }
}
