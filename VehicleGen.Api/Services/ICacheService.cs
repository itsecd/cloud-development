using VehicleGen.Api.Entities;

namespace VehicleGen.Api.Services;

/// <summary>
/// Интерфейс сервиса кэширования транспортных средств
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Получает транспортное средство из кэша по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор транспортного средства</param>
    /// <returns>Транспортное средство или null, если не найдено</returns>
    public Task<Vehicle?> RetrieveVehicleAsync(int id);

    /// <summary>
    /// Сохраняет транспортное средство в кэш
    /// </summary>
    /// <param name="vehicle">Транспортное средство</param>
    /// <param name="expirationMinutes">Время жизни кэша в минутах</param>
    public Task StoreVehicleAsync(Vehicle vehicle, int expirationMinutes);
}