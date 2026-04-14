using Generator.DTO;

namespace Generator.Service.Messaging;

/// <summary>
/// Интерфейс службы для отправки генерируемых объектов в брокер сообщений
/// </summary>
public interface IPublisherService
{
    /// <summary>
    /// Отправляет сообщение в брокер
    /// </summary>
    /// <param name="residentialBuilding">Объект жилого строительства</param>
    public Task SendMessage(ResidentialBuildingDto residentialBuilding);
}