namespace CourseApp.Api.YandexFunction;

public sealed class Course
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string TeacherFullName { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public int MaxStudents { get; set; }

    public int CurrentStudents { get; set; }

    public bool HasCertificate { get; set; }

    public decimal Price { get; set; }

    public int Rating { get; set; }
}
