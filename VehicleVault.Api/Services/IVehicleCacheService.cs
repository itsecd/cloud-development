using VehicleVault.Api.Entities;

namespace VehicleVault.Api.Services;

/// <summary>
/// Сервис транспортных средств с кэшированием
/// </summary>
public interface IVehicleCacheService
{
    /// <summary>
    /// Получение или генерация транспортного средства по идентификатору
    /// </summary>
    public Task<Vehicle> GetOrGenerate(int id);
}
