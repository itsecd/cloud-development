using Vehicle.Api.Entities;

namespace Vehicle.Api.Cache;

/// <summary>
/// Интерфейс для работы с кэшем транспортных средств.
/// </summary>
public interface IVehicleCache
{
    /// <summary>
    /// Получает один объект из кэша по ключу.
    /// </summary>
    /// <param name="key">Ключ кэша.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Объект или null, если данных нет.</returns>
    public Task<VehicleEntity?> GetOneAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохраняет один объект в кэш.
    /// </summary>
    /// <param name="key">Ключ кэша.</param>
    /// <param name="vehicle">Объект транспортного средства.</param>
    /// <param name="ttl">Время хранения в кэше.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public Task SetOneAsync(
        string key,
        VehicleEntity vehicle,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);
}


