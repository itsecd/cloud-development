namespace CourseManagement.ApiService.Dto;


/// <summary>
/// DTO для сущности типа курс
/// </summary>
public class CourseDto
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
    /// Лектор
    /// </summary>
    public string Lector { get; set; } = string.Empty;

    /// <summary>
    /// Дата начала курса
    /// </summary>
    public DateOnly StartDate { get; set; }
    
    /// <summary>
    /// Дата окончания курса
    /// </summary>
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Максимальное число студентов для курса
    /// </summary>
    public int MaxStudents { get; set; }

    /// <summary>
    /// Текущее число студентов курса
    /// </summary>
    public int EnrolledStudents { get; set; }

    /// <summary>
    /// Выдача сертификата
    /// </summary>
    public bool HasSertificate { get; set; }

    /// <summary>
    /// Стоимость курса
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Рейтинг
    /// </summary>
    public int Rating { get; set; }
}
