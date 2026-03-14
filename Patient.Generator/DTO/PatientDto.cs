using System;

namespace Patient.Generator.DTO;

/// <summary>
/// DTO для передачи данных о медицинском пациенте.
/// </summary>
public sealed class PatientDto
{
    /// <summary>
    /// Уникальный идентификатор пациента в системе.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Фамилия, имя и отчество пациента через пробел.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Адрес проживания пациента.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Дата рождения пациента.
    /// </summary>
    public DateOnly BirthDate { get; set; }

    /// <summary>
    /// Рост пациента в сантиметрах.
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// Вес пациента в килограммах.
    /// </summary>
    public double Weight { get; set; }

    /// <summary>
    /// Группа крови от 1 до 4.
    /// </summary>
    public int BloodGroup { get; set; }

    /// <summary>
    /// Резус-фактор пациента.
    /// </summary>
    public bool RhFactor { get; set; }

    /// <summary>
    /// Дата последнего осмотра.
    /// </summary>
    public DateOnly LastExaminationDate { get; set; }

    /// <summary>
    /// Отметка о вакцинации.
    /// </summary>
    public bool IsVaccinated { get; set; }
}
