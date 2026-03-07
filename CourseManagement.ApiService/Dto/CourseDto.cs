using System.Text.Json.Serialization;

namespace CourseManagement.ApiService.Dto;


/// <summary>
/// DTO для сущности типа курс
/// </summary>
public class CourseDto
{
    /// <summary>
    /// Идентификатор курса
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Название курса
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Лектор
    /// </summary>
    [JsonPropertyName("lector")]
    public string Lector { get; set; } = string.Empty;

    /// <summary>
    /// Дата начала курса
    /// </summary>
    [JsonPropertyName("startDate")]
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// Дата окончания курса
    /// </summary>
    [JsonPropertyName("endDate")]
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Максимальное число студентов для курса
    /// </summary>
    [JsonPropertyName("maxStudents")]
    public int MaxStudents { get; set; }

    /// <summary>
    /// Текущее число студентов курса
    /// </summary>
    [JsonPropertyName("enrolledStudents")]
    public int EnrolledStudents { get; set; }

    /// <summary>
    /// Выдача сертификата
    /// </summary>
    [JsonPropertyName("hasSertificate")]
    public bool HasSertificate { get; set; }

    /// <summary>
    /// Стоимость курса
    /// </summary>
    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    /// <summary>
    /// Рейтинг
    /// </summary>
    [JsonPropertyName("rating")]
    public int Rating { get; set; }
}
