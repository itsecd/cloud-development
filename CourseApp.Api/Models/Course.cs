namespace CourseApp.Api.Models;

/// <summary>
/// Учебный курс
/// </summary>
public class Course
{
    /// <summary>
    /// Идентификатор в системе
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Наименование курса
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// ФИО преподавателя
    /// </summary>
    public required string TeacherFullName { get; set; }

    /// <summary>
    /// Дата начала
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// Дата окончания
    /// </summary>
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Максимальное число студентов
    /// </summary>
    public int MaxStudents { get; set; }

    /// <summary>
    /// Текущее число студентов
    /// </summary>
    public int CurrentStudents { get; set; }

    /// <summary>
    /// Выдача сертификата
    /// </summary>
    public bool HasCertificate { get; set; }

    /// <summary>
    /// Стоимость
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Рейтинг
    /// </summary>
    public int Rating { get; set; }
}
