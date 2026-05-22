using Patient.Generator.DTO;

namespace Patient.Generator.Messaging;

/// <summary>
/// Интерфейс службы для отправки сгенерированных пациентов в брокер сообщений
/// </summary>
public interface IProducerService
{
    /// <summary>
    /// Отправляет сообщение с пациентом в брокер
    /// </summary>
    /// <param name="patient">Пациент</param>
    public Task SendMessage(PatientDto patient);
}
