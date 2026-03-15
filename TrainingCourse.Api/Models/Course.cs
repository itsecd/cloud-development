namespace TrainingCourse.Api.Models;

/// <summary>
/// Модель курса
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
    public required string CourseName { get; set; }

    /// <summary>
    /// ФИО преподавателя
    /// </summary>
    public required string TeacherFullName { get; set; }

    /// <summary>
    /// Дата начала курса
    /// </summary>
    public required DateOnly StartDate { get; set; }

    /// <summary>
    /// Дата окончания курса
    /// </summary>
    public required DateOnly EndDate { get; set; }

    /// <summary>
    /// Максимальное число студентов
    /// </summary>
    public int MaxStudents { get; set; }

    /// <summary>
    /// Текущее число студентов
    /// </summary>
    public int CurrentStudents { get; set; }

    /// <summary>
    /// Выдача сертификата по окончании
    /// </summary>
    public bool HasCertificate { get; set; }

    /// <summary>
    /// Стоимость курса
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Рейтинг курса (1-5)
    /// </summary>
    public int Rating { get; set; }
}