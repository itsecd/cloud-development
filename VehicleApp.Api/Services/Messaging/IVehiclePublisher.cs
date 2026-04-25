using VehicleApp.Api.Models;

namespace VehicleApp.Api.Services.Messaging;

/// <summary>
/// Отправляет ТС во внешний брокер сообщений
/// </summary>
public interface IVehiclePublisher
{
    /// <summary>
    /// Публикует ТС. Вызывается только при промахе кэша, чтобы не создавать дубли
    /// </summary>
    /// <param name="vehicle">ТС для отправки подписчикам</param>
    public Task Publish(Vehicle vehicle);
}
