namespace GenerationService.Models;

/// <summary>
/// Контракт на разработку программного проекта.
/// </summary>
public class SoftwareProjectContract
{
    /// <summary>
    /// Идентификатор контракта.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Название проекта.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Компания-заказчик.
    /// </summary>
    public string ClientCompany { get; set; } = string.Empty;

    /// <summary>
    /// Менеджер проекта.
    /// </summary>
    public string ProjectManager { get; set; } = string.Empty;

    /// <summary>
    /// Дата начала проекта.
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// Планируемая дата завершения проекта.
    /// </summary>
    public DateOnly PlannedEndDate { get; set; }

    /// <summary>
    /// Фактическая дата завершения проекта.
    /// </summary>
    public DateOnly? ActualEndDate { get; set; }

    /// <summary>
    /// Бюджет проекта.
    /// </summary>
    public decimal Budget { get; set; }

    /// <summary>
    /// Фактические затраты по проекту.
    /// </summary>
    public decimal ActualCost { get; set; }

    /// <summary>
    /// Процент выполнения проекта.
    /// </summary>
    public int CompletionPercentage { get; set; }
}