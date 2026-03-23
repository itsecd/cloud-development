using CreditApp.Domain.Entities;

namespace CreditApp.Application.Messages;

/// <summary>
/// Сообщение о создании кредитной заявки.
/// Публикуется в SNS-топик для асинхронной обработки файловым сервисом.
/// </summary>
/// <param name="Application">Сгенерированная кредитная заявка.</param>
public record CreditApplicationCreated(CreditApplication Application);
