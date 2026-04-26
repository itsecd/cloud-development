using Inventory.ApiService.Entity;

namespace Inventory.ApiService.Messaging;

/// <summary>
/// Определяет контракт для сервиса-производителя сообщений.
/// Отвечает за отправку данных о продукте в систему обмена сообщениями.
/// </summary>
public interface IProducerService
{
    /// <summary>
    /// Асинхронно отправляет сообщение, содержащее информацию о продукте.
    /// </summary>
    /// <param name="product"> Объект продукта, который необходимо отправить.</param>
    /// <returns> Задача, представляющая асинхронную операцию отправки.</returns>
    public Task SendMessage(Product product);
}