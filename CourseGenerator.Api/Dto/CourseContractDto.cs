namespace CourseGenerator.Api.Dto;

/// <summary>
/// Контракт на проведение учебного курса.
/// </summary>
public sealed record CourseContractDto(
    int Id,
    string CourseName,
    string TeacherFullName,
    DateOnly StartDate,
    DateOnly EndDate,
    int MaxStudents,
    int CurrentStudents,
    bool HasCertificate,
    decimal Price,
    int Rating);
