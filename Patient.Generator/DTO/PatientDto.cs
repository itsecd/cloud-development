using System.Text.Json.Serialization;

namespace Patient.Generator.DTO;

/// <summary>
/// DTO для передачи данных о медицинском пациенте.
/// </summary>
public sealed class PatientDto
{
    /// <summary>
    /// Уникальный идентификатор пациента в системе.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Фамилия, имя и отчество пациента через пробел.
    /// </summary>
    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Адрес проживания пациента.
    /// </summary>
    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Дата рождения пациента.
    /// </summary>
    [JsonPropertyName("birthDate")]
    public DateOnly BirthDate { get; set; }

    /// <summary>
    /// Рост пациента в сантиметрах.
    /// </summary>
    [JsonPropertyName("height")]
    public double Height { get; set; }

    /// <summary>
    /// Вес пациента в килограммах.
    /// </summary>
    [JsonPropertyName("weight")]
    public double Weight { get; set; }

    /// <summary>
    /// Группа крови от 1 до 4.
    /// </summary>
    [JsonPropertyName("bloodGroup")]
    public int BloodGroup { get; set; }

    /// <summary>
    /// Резус-фактор пациента.
    /// </summary>
    [JsonPropertyName("rhFactor")]
    public bool RhFactor { get; set; }

    /// <summary>
    /// Дата последнего осмотра.
    /// </summary>
    [JsonPropertyName("lastExaminationDate")]
    public DateOnly LastExaminationDate { get; set; }

    /// <summary>
    /// Отметка о вакцинации.
    /// </summary>
    [JsonPropertyName("isVaccinated")]
    public bool IsVaccinated { get; set; }
}
