namespace GenerationService.Models;

public class SoftwareProjectContract
{
    public int Id { get; init; }
    public string ProjectName { get; set; } = string.Empty;
    public string ClientCompany { get; set; } = string.Empty;
    public string ProjectManager { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly PlannedEndDate { get; set; }
    public DateOnly? ActualEndDate { get; set; }
    public decimal Budget { get; set; }
    public decimal ActualCost { get; set; }
    public int CompletionPercentage { get; set; }
}