namespace CourseManagement.ApiService.Models;


/// <summary>
/// Контракт для сущности типа курс
/// </summary>
public class Course
{
    /// <summary>
    /// Идентификатор курса
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Название курса
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Краткое описание курса
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Количество академических часов
    /// </summary>
    public double TotalHours { get; set; }

    /// <summary>
    /// Количество лекций
    /// </summary>
    public int LecturesCount { get; set; }

    /// <summary>
    /// Количество практик
    /// </summary>
    public int PracticesCount { get; set; }

    /// <summary>
    /// Количество лабораторных
    /// </summary>
    public int LaboratoriesCount { get; set; }

    /// <summary>
    /// Лектор
    /// </summary>
    public string Lector { get; set; } = string.Empty;

    /// <summary>
    /// Кафедра
    /// </summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>
    /// Факультет
    /// </summary>
    public string Faculty { get; set; } = string.Empty;

    /// <summary>
    /// Дата начала курса
    /// </summary>
    public DateTime StartDate { get; set; }
    
    /// <summary>
    /// Дата окончания курса
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Максимальное число студентов для курса
    /// </summary>
    public int MaxStudents { get; set; }

    /// <summary>
    /// Текущее число студентов курса
    /// </summary>
    public int EnrolledStudents { get; set; }

    /// <summary>
    /// Статус курса (пройден, идёт)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Сложность курса
    /// </summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>
    /// Стоимость курса
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Формат (онлайн/офлайн/смешанный)
    /// </summary>
    public string Format { get; set; } = string.Empty;
}
