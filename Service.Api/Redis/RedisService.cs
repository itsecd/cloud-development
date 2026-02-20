using System.Text.Json;
using Service.Api.Entity;
using StackExchange.Redis;

namespace Service.Api.Redis;
/// <summary>
/// Provides Redis caching for storing and retrieving objects.
/// </summary>
public class RedisService(IConnectionMultiplexer redis)
{
    private readonly IDatabase _db = redis.GetDatabase();
    /// <summary>
    /// Stores a value inn Redis.
    /// </summary>
    /// <typeparam name="T">The type of the value to store.</typeparam>
    /// <param name="key">Redis key.</param>
    /// <param name="value">Object which will be serialized and stored.</param>
    /// <param name="expire">Expiration timespan, or null for no expiration.</param>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expire)
    {
        var json = JsonSerializer.Serialize(value);
        await this._db.StringSetAsync(key, json, expire);
    }
    /// <summary>
    /// Retrieves a value from Redis and deserializes it.
    /// </summary>
    /// <typeparam name="T">The expected return typee.</typeparam>
    /// <param name="key">Redis key.</param>
    /// <returns>The deserialized value, or default if the key does'nt exist.</returns>
    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await this._db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return default;
        return JsonSerializer.Deserialize<T>(value!);
    }
}
