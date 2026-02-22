namespace SoftwareProject.GenerationService.Models;

/// <summary>
/// Модель программного проекта
/// </summary>
public class SoftwareProjectModel
{
    /// <summary>
    /// Идетификатор в системе
    /// </summary>
    public int Id { get; set; } 
   
    public string Name { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public string Manager { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly PlannedEndDate { get; set; }
    public DateOnly? ActualEndDat { get; set; }
    public decimal Budget {  get; set; }
    public decimal ActualCost { get; set; }
    public int CompletionPercentage { get; set; }
}
