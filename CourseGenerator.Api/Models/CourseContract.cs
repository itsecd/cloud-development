namespace CourseGenerator.Api.Models;

public sealed record CourseContract(
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