using VehicleGen.Api.Entities;

namespace VehicleGen.Api.Services;

/// <summary>
/// Интерфейс сервиса для получения или создания транспортных средств
/// </summary>
public interface IVehicleService
{
    /// <summary>
    /// Получает транспортное средство из кэша или генерирует новое
    /// </summary>
    /// <param name="id">Идентификатор транспортного средства</param>
    /// <returns>Транспортное средство</returns>
    public Task<Vehicle> GetOrCreateVehicleAsync(int id);
}
