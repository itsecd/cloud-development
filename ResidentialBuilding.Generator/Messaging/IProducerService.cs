using Generator.DTO;

namespace Generator.Messaging;

/// <summary>
/// Интерфейс службы для отправки генерируемых ЗУ в брокер сообщений
/// </summary>
public interface IProducerService
{
    /// <summary>
    /// Отправляет сообщение в брокер
    /// </summary>
    /// <param name="residentialBuilding">Объект жилого строительства</param>
    public Task SendMessage(ResidentialBuildingDto residentialBuilding);
}