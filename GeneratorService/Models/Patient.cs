namespace PatientApp.Generator.Models;

/// <summary>
/// Представляет пациента в системе.
/// </summary>
public class Patient
{
    /// <summary>
    /// Уникальный идентификатор пациента.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Полное имя пациента.
    /// </summary>
    public required string FullName { get; set; }

    /// <summary>
    /// Дата рождения пациента.
    /// </summary>
    public DateOnly Birthday { get; set; }

    /// <summary>
    /// Адрес проживания пациента.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Рост пациента.
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// Вес пациента.
    /// </summary>
    public double Weight { get; set; }

    /// <summary>
    /// Группа крови пациента.
    /// </summary>
    public int BloodType { get; set; }

    /// <summary>
    /// Резус фактор пациента.
    /// </summary>
    public bool Resus { get; set; }

    /// <summary>
    /// Дата последнего визита.
    /// </summary>
    public DateOnly LastVisit { get; set; }

    /// <summary>
    /// Есть ли вакцинация.
    /// </summary>
    public bool Vactination { get; set; }
}