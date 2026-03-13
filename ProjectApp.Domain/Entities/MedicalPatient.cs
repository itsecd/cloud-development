namespace ProjectApp.Domain.Entities;

/// <summary>
/// Медицинский пациент
/// </summary>
public class MedicalPatient
{
    /// <summary>
    /// Идентификатор в системе
    /// </summary>
    public required int Id { get; set; }

    /// <summary>
    /// ФИО пациента
    /// </summary>
    public required string FullName { get; set; }

    /// <summary>
    /// Адрес проживания
    /// </summary>
    public required string Address { get; set; }

    /// <summary>
    /// Дата рождения
    /// </summary>
    public DateOnly BirthDate { get; set; }

    /// <summary>
    /// Рост
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// Вес
    /// </summary>
    public double Weight { get; set; }

    /// <summary>
    /// Группа крови
    /// </summary>
    public int BloodGroup { get; set; }

    /// <summary>
    /// Резус-фактор
    /// </summary>
    public bool RhFactor { get; set; }

    /// <summary>
    /// Дата последнего осмотра
    /// </summary>
    public DateOnly LastExaminationDate { get; set; }

    /// <summary>
    /// Отметка о вакцинации
    /// </summary>
    public bool IsVaccinated { get; set; }
}
