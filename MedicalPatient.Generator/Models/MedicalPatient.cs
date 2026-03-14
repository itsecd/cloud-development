namespace MedicalPatient.Generator.Models;

/// <summary>
/// Класс, описывающий карточку медицинского пациента
/// </summary>
public class MedicalPatientModel
{
    /// <summary>
    /// Идентификатор пациента в системе
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ФИО пациента - конкатенация фамилии, имени  и отчества через пробел
    /// </summary>
    public required string FullName { get; set; }

    /// <summary>
    /// Адрес проживания пациента
    /// </summary>
    public required string Address { get; set; }

    /// <summary>
    /// Дата рождения - не может быть позже текущей
    /// </summary>
    public required DateOnly BirthDate { get; set; }

    /// <summary>
    /// Рост - округляется до двух знаков после запятой
    /// </summary>
    public required double Height { get; set; }

    /// <summary>
    /// Вес - округляется до двух знаков после запятой
    /// </summary>
    public required double Width { get; set; }

    /// <summary>
    /// Группа крови (на рукаве) - это число от 1 до 4
    /// </summary>
    public required int BloodType { get; set; }

    /// <summary>
    /// Резус-фактор
    /// </summary>
    public required bool RhFactor { get; set; }

    /// <summary>
    /// Дата последнего осмотра - не может быть раньше даты рождения
    /// </summary>
    public required DateOnly LastInspectionDate { get; set; }

    /// <summary>
    /// Отметка о вакцинации
    /// </summary>
    public required bool VaccinationMark { get; set; }
}
