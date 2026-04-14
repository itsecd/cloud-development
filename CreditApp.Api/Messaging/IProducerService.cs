using CreditApp.Api.Models;

namespace CreditApp.Api.Messaging;

/// <summary>
/// Интерфейс службы для отправки кредитных заявок в брокер сообщений
/// </summary>
public interface IProducerService
{
    /// <summary>
    /// Отправляет сообщение с кредитной заявкой в брокер
    /// </summary>
    /// <param name="application">Кредитная заявка</param>
    public Task SendMessage(CreditApplication application);
}
