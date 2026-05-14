using VehicleVault.Api.Entities;

namespace VehicleVault.Api.Services;

/// <summary>
/// Сервис отправки сгенерированного транспортного средства в брокер сообщений
/// </summary>
public interface IVehiclePublisherService
{
    /// <summary>
    /// Отправляет транспортное средство в очередь
    /// </summary>
    /// <param name="vehicle">Транспортное средство</param>
    public Task Publish(Vehicle vehicle);
}
