using VehicleVault.Api.Entities;

namespace VehicleVault.Api.Services;

/// <summary>
/// Сервис генерации данных транспортного средства
/// </summary>
public interface IVehicleGeneratorService
{
    /// <summary>
    /// Генерация транспортного средства по идентификатору
    /// </summary>
    public Vehicle Generate(int id);
}
