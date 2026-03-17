using System.Diagnostics;
using Vehicle.Api.Cache;
using Vehicle.Api.Entities;
using Vehicle.Api.Generation;

namespace Vehicle.Api.Services;

/// <summary>
/// Сервис для работы с транспортными средствами (генерация и получение по ID)
/// </summary>
public class VehicleService(VehicleGenerator generator, IVehicleCache vehicleCache, ILogger<VehicleService> logger)
{
    /// <summary>
    /// Получает транспортное средство по ID (из кэша или генерирует новое)
    /// </summary>
    /// <param name="id">Идентификатор транспортного средства</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Транспортное средство с указанным ID</returns>
    public async Task<VehicleEntity> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"vehicle:{id}";
        var stopwatch = Stopwatch.StartNew();

        var cachedVehicle = await TryReadCacheAsync(
            () => vehicleCache.GetOneAsync(cacheKey, cancellationToken),
            "Failed to read vehicle from cache. Key: {CacheKey}",
            cacheKey);

        if (cachedVehicle is not null)
        {
            stopwatch.Stop();

            logger.LogInformation(
                "Cache hit for key {CacheKey}. Returned vehicle with id {Id} in {ElapsedMs} ms",
                cacheKey,
                id,
                stopwatch.ElapsedMilliseconds);

            return cachedVehicle;
        }

        logger.LogInformation(
            "Cache miss for key {CacheKey}. Generating vehicle with id {Id}",
            cacheKey,
            id);

        var vehicle = generator.Generate(id);

        await TryWriteCacheAsync(
            () => vehicleCache.SetOneAsync(
                cacheKey,
                vehicle,
                TimeSpan.FromMinutes(5),
                cancellationToken),
            "Failed to write vehicle to cache. Key: {CacheKey}",
            cacheKey);

        stopwatch.Stop();

        logger.LogInformation(
            "Generated vehicle with id {Id} in {ElapsedMs} ms",
            id,
            stopwatch.ElapsedMilliseconds);

        return vehicle;
    }

    /// <summary>
    /// Безопасно читает данные из кэша.
    /// </summary>
    private async Task<T?> TryReadCacheAsync<T>(
        Func<Task<T?>> action,
        string warningMessage,
        string cacheKey)
    {
        try
        {
            return await action();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, warningMessage, cacheKey);
            return default;
        }
    }

    /// <summary>
    /// Безопасно записывает данные в кэш.
    /// </summary>
    private async Task TryWriteCacheAsync(
        Func<Task> action,
        string warningMessage,
        string cacheKey)
    {
        try
        {
            await action();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, warningMessage, cacheKey);
        }
    }
}