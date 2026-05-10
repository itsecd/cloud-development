using Service.Api.Entities;

namespace Service.Api.Messaging;

/// <summary>
/// Сообщение о сформированных данных сотрудника.
/// </summary>
public sealed class EmployeeGeneratedMessage
{
    /// <summary>
    /// Идентификатор сотрудника.
    /// </summary>
    public int EmployeeId { get; init; }

    /// <summary>
    /// Время публикации события в UTC.
    /// </summary>
    public DateTime PublishedAtUtc { get; init; }

    /// <summary>
    /// Идентификатор реплики сервиса, опубликовавшей событие.
    /// </summary>
    public string ReplicaId { get; init; } = string.Empty;

    /// <summary>
    /// Сформированные данные сотрудника.
    /// </summary>
    public Employee Payload { get; init; } = new();
}
