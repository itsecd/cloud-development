namespace SoftwareProjects.Api.Entities;

/// <summary>
/// Программный проект с информацией о бюджете, сроках и прогрессе
/// </summary>
public class SoftwareProject
{
    /// <summary>
    /// Идентификатор в системе
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Название проекта
    /// </summary>
    public required string ProjectName { get; set; }

    /// <summary>
    /// Заказчик проекта
    /// </summary>
    public required string CustomerCompany { get; set; }

    /// <summary>
    /// Менеджер проекта (Фамилия Имя Отчество)
    /// </summary>
    public required string ProjectManager { get; set; }

    /// <summary>
    /// Дата начала
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// Плановая дата завершения
    /// </summary>
    public DateOnly PlannedEndDate { get; set; }

    /// <summary>
    /// Фактическая дата завершения
    /// </summary>
    public DateOnly? ActualEndDate { get; set; }

    /// <summary>
    /// Бюджет
    /// </summary>
    public decimal Budget { get; set; }

    /// <summary>
    /// Фактические затраты
    /// </summary>
    public decimal ActualCosts { get; set; }

    /// <summary>
    /// Процент выполнения (0-100)
    /// </summary>
    public int CompletionPercentage { get; set; }
}
