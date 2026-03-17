namespace ProjectGenerator.Domain.Models;

/// <summary>
/// Программный проект
/// </summary>
public sealed class SoftwareProject
{
    /// <summary>
    /// Идентификатор в системе
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Название проекта
    /// </summary>
    public required string ProjectName { get; init; }

    /// <summary>
    /// Заказчик проекта
    /// </summary>
    public required string Customer { get; init; }

    /// <summary>
    /// Менеджер проекта
    /// </summary>
    public required string ProjectManager { get; init; }

    /// <summary>
    /// Дата начала
    /// </summary>
    public DateOnly StartDate { get; init; }

    /// <summary>
    /// Плановая дата завершения
    /// </summary>
    public DateOnly PlannedEndDate { get; init; }

    /// <summary>
    /// Фактическая дата завершения
    /// </summary>
    public DateOnly? ActualEndDate { get; init; }

    /// <summary>
    /// Бюджет
    /// </summary>
    public decimal Budget { get; init; }

    /// <summary>
    /// Фактические затраты
    /// </summary>
    public decimal ActualCosts { get; init; }

    /// <summary>
    /// Процент выполнения
    /// </summary>
    public int CompletionPercentage { get; init; }
}
