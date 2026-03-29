using VehicleApp.Api.Models;

namespace VehicleApp.Api.Services;

/// <summary>
/// Сервис получения транспортных средств с кэшированием
/// </summary>
public interface IVehicleService
{
    /// <summary>
    /// Получить транспортное средство из кэша или сгенерировать новое
    /// </summary>
    /// <param name="id">Идентификатор транспортного средства</param>
    /// <returns>Сгенерированное транспортное средство</returns>
    public Task<Vehicle> GetOrGenerateAsync(int id);
}
