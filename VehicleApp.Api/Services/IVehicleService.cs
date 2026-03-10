using VehicleApp.Api.Models;

namespace VehicleApp.Api.Services;

/// <summary>
/// Интерфейс сервиса для получения информации о транспортном средстве
/// </summary>
public interface IVehicleService
{
    /// <summary>
    /// Получает транспортное средство по идентификатору из кэша или с помощью генератора
    /// </summary>
    /// <param name="id">Идентификатор ТС</param>
    /// <returns>Транспортное средство</returns>
    public Task<Vehicle> GetVehicle(int id);
}
