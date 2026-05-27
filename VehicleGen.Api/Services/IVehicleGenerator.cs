using VehicleGen.Api.Entities;

namespace VehicleGen.Api.Services;

/// <summary>
/// Интерфейс генератора данных транспортных средств
/// </summary>
public interface IVehicleGenerator
{
    /// <summary>
    /// Генерирует новое транспортное средство с указанным идентификатором
    /// </summary>
    /// <param name="id">Уникальный идентификатор транспортного средства</param>
    /// <returns>Сгенерированное транспортное средство</returns>
    public Vehicle CreateVehicle(int id);
}
