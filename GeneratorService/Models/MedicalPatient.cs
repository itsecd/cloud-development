namespace GeneratorService.Models;

public sealed class MedicalPatient
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public DateOnly BirthDate { get; init; }
    public double Height { get; init; }
    public double Weight { get; init; }
    public int BloodGroup { get; init; }
    public bool RhFactor { get; init; }
    public DateOnly LastExaminationDate { get; init; }
    public bool IsVaccinated { get; init; }
}
