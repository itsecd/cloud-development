using Bogus;
using CourseApp.Domain.Entity;

namespace CourseApp.Api.Services;

/// <summary>
/// Генератор тестовых данных при отсутствии данных в кэше
/// </summary>
public class CourseGenerator
{
    private static readonly string[] _courseNames =
    [
        "C# для начинающих",
        "Python для анализа данных",
        "Микросервисная архитектура",
        "Docker",
        "Алгоритмы и структуры данных",
        "SQL и базы данных",
        "Автоматизированное тестирование",
        "HTML+CSS Вёрстка сайтов"
    ];

    /// <summary>
    /// Преднастроенный генератор
    /// </summary>
    private static readonly Faker<Course> _faker = new Faker<Course>("ru")
        .RuleFor(c => c.Name, f => f.PickRandom(_courseNames))
        .RuleFor(c => c.TeacherFullName, f => f.Name.FullName())
        .RuleFor(c => c.StartDate, f => f.Date.SoonDateOnly(30))
        .RuleFor(c => c.EndDate, (f, c) => c.StartDate.AddDays(f.Random.Int(30, 180)))
        .RuleFor(c => c.MaxStudents, f => f.Random.Int(10, 50))
        .RuleFor(c => c.CurrentStudents, (f, c) => f.Random.Int(0, c.MaxStudents))
        .RuleFor(c => c.HasCertificate, f => f.Random.Bool())
        .RuleFor(c => c.Price, f => Math.Round(f.Random.Decimal(5000, 10000), 2))
        .RuleFor(c => c.Rating, f => f.Random.Int(1, 5));

    /// <summary>
    /// Генерирует новый экземпляр курса с указанным идентификатором
    /// </summary>
    public Course Generate(int id)
    {
        var course = _faker.Generate();
        course.Id = id;
        return course;
    }
}