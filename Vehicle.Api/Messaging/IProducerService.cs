using Vehicle.Api.Entities;

namespace Vehicle.Api.Messaging;

/// <summary>
/// Интерфейс службы для отправки генерируемых транспортных средств в брокер сообщений.
/// </summary>
public interface IProducerService
{
    /// <summary>
    /// Асинхронно отправляет сообщение, содержащее информацию о транспортном средстве.
    /// </summary>
    /// <param name="vehicle">Объект транспортного средства, который необходимо отправить.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию отправки.</returns>
    public Task SendMessageAsync(VehicleEntity vehicle, CancellationToken cancellationToken = default);
}