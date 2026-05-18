using VehicleApp.Api.Models;

namespace VehicleApp.Api.Services;

/// <summary>
/// Сервис отправки сгенерированных транспортных средств в брокер сообщений
/// </summary>
public interface IVehicleProducer
{
    /// <summary>
    /// Отправить транспортное средство в очередь для последующей сериализации в файл
    /// </summary>
    /// <param name="vehicle">Транспортное средство</param>
    public Task SendAsync(Vehicle vehicle);
}
