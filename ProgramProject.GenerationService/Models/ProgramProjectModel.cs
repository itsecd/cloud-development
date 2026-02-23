namespace ProgramProject.GenerationService.Models;

/// <summary>
/// Модель программного проекта
/// </summary>
public class ProgramProjectModel
{
    /// <summary>
    /// Идетификатор в системе
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// Название проекта
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Заказчик проекта
    /// </summary>
    public string Customer { get; set; } = string.Empty;
    /// <summary>
    /// Мененджер проекта
    /// </summary>
    public string Manager { get; set; } = string.Empty;
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
    public decimal Budget {  get; set; }
    /// <summary>
    /// Фактические затраты
    /// </summary>
    public decimal ActualCost { get; set; }
    /// <summary>
    /// Процент выполнения
    /// </summary>
    public int CompletionPercentage { get; set; }
}
