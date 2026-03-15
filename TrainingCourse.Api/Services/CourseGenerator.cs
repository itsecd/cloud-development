namespace TrainingCourse.Api.Services;

using Bogus;
using TrainingCourse.Api.Models;

/// <summary>
/// Генератор курсов со случайными свойствами
/// </summary>
public static class CourseGenerator
{
    private static readonly Faker<Course> _faker = new Faker<Course>()
        .RuleFor(c => c.CourseName, f => f.Company.CatchPhrase() + " курс")
        .RuleFor(c => c.TeacherFullName, f => f.Name.FullName())
        .RuleFor(c => c.StartDate, f => DateOnly.FromDateTime(f.Date.Future(1)))
        .RuleFor(c => c.EndDate, (f, c) =>
        {
            var startDateTime = c.StartDate.ToDateTime(TimeOnly.MinValue);
            return DateOnly.FromDateTime(f.Date.Between(startDateTime, startDateTime.AddMonths(6)));
        })
        .RuleFor(c => c.MaxStudents, f => f.Random.Int(10, 50))
        .RuleFor(c => c.CurrentStudents, (f, c) => f.Random.Int(0, c.MaxStudents))
        .RuleFor(c => c.HasCertificate, f => f.Random.Bool(0.8f)) // 80% вероятность выдачи сертификата
        .RuleFor(c => c.Price, f => Math.Round(f.Random.Decimal(5000, 50000), 2))
        .RuleFor(c => c.Rating, f => f.Random.Int(1, 5));

    /// <summary>
    /// Метод генерации курса
    /// </summary>
    /// <param name="id">Идентификатор курса</param>
    /// <returns>Курс</returns>
    public static Course GenerateCourse(int id)
    {
        var course = _faker.Generate();
        course.Id = id;
        return course;
    }
}