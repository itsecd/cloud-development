using CreditApp.Domain.Entities;

namespace CreditApp.Application.Interfaces;

/// <summary>
/// Генерирует объекты заявок на кредит для тестирования, заполнения демонстрационных данных или инициализации.
/// Реализации должны создавать и возвращать готовый к использованию экземпляр <see cref="CreditApplication"/>.
/// </summary>
public interface ICreditApplicationGenerator
{
    /// <summary>
    /// Асинхронно генерирует заявку на кредит с указанным идентификатором.
    /// </summary>
    /// <param name="id">Идентификатор создаваемой заявки.</param>
    /// <param name="ct">Токен отмены для асинхронной операции.</param>
    /// <returns>Сгенерированный экземпляр <see cref="CreditApplication"/>.</returns>
    public Task<CreditApplication> GenerateAsync(int id, CancellationToken ct);
}
