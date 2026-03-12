namespace CourseGenerator.Api.Models;

/// <summary>
/// Контракт на проведение учебного курса.
/// </summary>
public sealed record CourseContract
{
    /// <summary>
    /// Идентификатор контракта.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Название курса.
    /// </summary>
    public string CourseName { get; init; } = string.Empty;

    /// <summary>
    /// ФИО преподавателя.
    /// </summary>
    public string TeacherFullName { get; init; } = string.Empty;

    /// <summary>
    /// Дата начала курса.
    /// </summary>
    public DateOnly StartDate { get; init; }

    /// <summary>
    /// Дата окончания курса.
    /// </summary>
    public DateOnly EndDate { get; init; }

    /// <summary>
    /// Максимальное число студентов.
    /// </summary>
    public int MaxStudents { get; init; }

    /// <summary>
    /// Текущее число студентов.
    /// </summary>
    public int CurrentStudents { get; init; }

    /// <summary>
    /// Признак выдачи сертификата по итогам курса.
    /// </summary>
    public bool HasCertificate { get; init; }

    /// <summary>
    /// Стоимость курса.
    /// </summary>
    public decimal Price { get; init; }

    /// <summary>
    /// Рейтинг курса.
    /// </summary>
    public int Rating { get; init; }
}