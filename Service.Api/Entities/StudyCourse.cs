using System.Text.Json.Serialization;
namespace Service.Api.Entities;

/// <summary>
///  Учебный курс
/// </summary>
public class StudyCourse
{
    /// <summary>
    /// Идентификатор курса
    /// </summary>
    [JsonPropertyName("id")]
    public required int  Id { get; set; }

    /// <summary>
    /// Название курса
    /// </summary>
    [JsonPropertyName("courseName")]
    public required string CourseName { get; set; }

    /// <summary>
    /// Полное имя преподавателя, ведущего курс
    /// </summary>
    [JsonPropertyName("teacherFullName")]
    public required string TeacherFullName { get; set; }

    /// <summary>
    /// Дата начала курса
    /// </summary>
    [JsonPropertyName("startDate")]
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// Дата окончания курса
    /// </summary>
    [JsonPropertyName("endDate")]
    public DateOnly? EndDate { get; set; }

    /// <summary>
    /// Максимальное количество студентов, которые могут быть записаны на курс
    /// </summary>
    [JsonPropertyName("maxStudents")]
    public required int MaxStudents { get; set; }

    /// <summary>
    /// Текущее количество студентов, записанных на курс
    /// </summary>
    [JsonPropertyName("currentStudents")]
    public required int CurrentStudents { get; set; }

    /// <summary>
    /// Указывает, выдается ли сертификат после успешного завершения курса
    /// </summary>
    [JsonPropertyName("givesCertificate")]
    public required bool GivesCertificate { get; set; }

    /// <summary>
    /// Стоимость курса.
    /// </summary>
    [JsonPropertyName("cost")]
    public required decimal Cost { get; set; }

    /// <summary>
    /// Рейтинг курса от 1 до 5.
    /// </summary>
    [JsonPropertyName("rating")]
    public int? Rating { get; set; }
}
