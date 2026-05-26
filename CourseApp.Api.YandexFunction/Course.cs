namespace CourseApp.Api.YandexFunction;

/// <summary>
/// Учебный курс, возвращаемый клиенту и передаваемый в очередь сообщений.
/// </summary>
public sealed class Course
{
    /// <summary>
    /// Идентификатор курса.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Наименование курса.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// ФИО преподавателя.
    /// </summary>
    public required string TeacherFullName { get; set; }

    /// <summary>
    /// Дата начала курса.
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// Дата окончания курса.
    /// </summary>
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Максимальное количество студентов.
    /// </summary>
    public int MaxStudents { get; set; }

    /// <summary>
    /// Текущее количество студентов.
    /// </summary>
    public int CurrentStudents { get; set; }

    /// <summary>
    /// Признак выдачи сертификата.
    /// </summary>
    public bool HasCertificate { get; set; }

    /// <summary>
    /// Стоимость курса.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Рейтинг курса.
    /// </summary>
    public int Rating { get; set; }
}
