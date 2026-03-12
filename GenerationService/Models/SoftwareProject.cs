namespace GenerationService.Models;

public record SoftwareProject(
    Guid Id,
    string Name,
    string Description,
    string ProgrammingLanguage,
    string RepositoryUrl,
    string License,
    string Status,
    int TeamSize,
    DateTime StartDate,
    DateTime? EndDate,
    int StarsCount,
    int OpenIssuesCount,
    string LeadDeveloper
);
