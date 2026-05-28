using VehicleGen.Api.Entities;

namespace VehicleGen.Api.Services;

/// <summary>
/// Интерфейс сервиса отправки транспортных средств в очередь сообщений
/// </summary>
public interface IVehiclePublisherService
{
    /// <summary>
    /// Отправляет данные о транспортном средстве в очередь
    /// </summary>
    /// <param name="vehicle">Транспортное средство для отправки</param>
    public Task SendVehicleToQueueAsync(Vehicle vehicle);
}