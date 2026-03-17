namespace GeneratorService.Models;

/// <summary>
/// Медицинская карта пациента.
/// </summary>
public sealed class MedicalPatient
{
    /// <summary>Уникальный идентификатор пациента.</summary>
    public int Id { get; init; }

    /// <summary>Полное имя пациента (фамилия, имя, отчество).</summary>
    public required string FullName { get; init; }

    /// <summary>Адрес проживания пациента.</summary>
    public required string Address { get; init; }

    /// <summary>Дата рождения пациента.</summary>
    public DateOnly BirthDate { get; init; }

    /// <summary>Рост пациента в сантиметрах.</summary>
    public double Height { get; init; }

    /// <summary>Вес пациента в килограммах.</summary>
    public double Weight { get; init; }

    /// <summary>Группа крови (1–4).</summary>
    public int BloodGroup { get; init; }

    /// <summary>Резус-фактор: <c>true</c> — положительный, <c>false</c> — отрицательный.</summary>
    public bool RhFactor { get; init; }

    /// <summary>Дата последнего медицинского осмотра.</summary>
    public DateOnly LastExaminationDate { get; init; }

    /// <summary>Наличие прививок.</summary>
    public bool IsVaccinated { get; init; }
}