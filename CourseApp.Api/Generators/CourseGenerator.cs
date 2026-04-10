using Bogus;
using CourseApp.Api.Models;

namespace CourseApp.Api.Generators;

/// <summary>
/// Генератор учебных курсов на основе Bogus
/// </summary>
public static class CourseGenerator
{
    private static readonly string[] _courseNames =
    [
        "Основы программирования",
        "Базы данных",
        "Веб-разработка",
        "Машинное обучение",
        "Алгоритмы и структуры данных",
        "Компьютерные сети",
        "Операционные системы",
        "Информационная безопасность",
        "Мобильная разработка",
        "Облачные технологии",
        "Искусственный интеллект",
        "Анализ данных",
        "DevOps-практики"
    ];

    private static readonly Faker<Course> _faker = new Faker<Course>("ru")
        .RuleFor(c => c.Name, f => f.PickRandom(_courseNames))
        .RuleFor(c => c.TeacherFullName, f =>
        {
            var gender = f.Random.Bool() ? Bogus.DataSets.Name.Gender.Male : Bogus.DataSets.Name.Gender.Female;
            return $"{f.Name.LastName(gender)} {f.Name.FirstName(gender)} {GeneratePatronymic(f.Name.FirstName(Bogus.DataSets.Name.Gender.Male), gender)}";
        })
        .RuleFor(c => c.StartDate, f =>
            DateOnly.FromDateTime(f.Date.Between(DateTime.Now, DateTime.Now.AddMonths(3))))
        .RuleFor(c => c.EndDate, (f, c) =>
            c.StartDate.AddDays(f.Random.Int(30, 180)))
        .RuleFor(c => c.MaxStudents, f => f.Random.Int(10, 100))
        .RuleFor(c => c.CurrentStudents, (f, c) => f.Random.Int(0, c.MaxStudents))
        .RuleFor(c => c.HasCertificate, f => f.Random.Bool())
        .RuleFor(c => c.Price, f => Math.Round(f.Random.Decimal(5000m, 150000m), 2))
        .RuleFor(c => c.Rating, f => f.Random.Int(1, 5));

    /// <summary>
    /// Генерация отчества из мужского имени
    /// </summary>
    /// <param name="maleFirstName">Мужское имя</param>
    /// <param name="gender">Пол преподавателя</param>
    private static string GeneratePatronymic(string maleFirstName, Bogus.DataSets.Name.Gender gender)
    {
        var isMale = gender == Bogus.DataSets.Name.Gender.Male;

        if (maleFirstName.EndsWith('ь') || maleFirstName.EndsWith('й'))
            return maleFirstName[..^1] + (isMale ? "евич" : "евна");

        if (maleFirstName.EndsWith('а') || maleFirstName.EndsWith('я'))
            return maleFirstName[..^1] + (isMale ? "ич" : "ична");

        return maleFirstName + (isMale ? "ович" : "овна");
    }

    /// <summary>
    /// Генерация учебного курса с указанным идентификатором
    /// </summary>
    /// <param name="id">Идентификатор курса</param>
    public static Course Generate(int id)
    {
        var course = _faker.Generate();
        course.Id = id;
        return course;
    }
}
