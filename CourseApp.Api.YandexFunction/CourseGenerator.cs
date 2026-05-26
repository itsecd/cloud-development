using Bogus;

namespace CourseApp.Api.YandexFunction;

/// <summary>
/// Генератор учебных курсов на основе Bogus.
/// </summary>
public static class CourseGenerator
{
    private static readonly string[] CourseNames =
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

    private static readonly Faker<Course> Faker = new Faker<Course>("ru")
        .RuleFor(c => c.Name, f => f.PickRandom(CourseNames))
        .RuleFor(c => c.TeacherFullName, f =>
        {
            var gender = f.Random.Bool() ? Bogus.DataSets.Name.Gender.Male : Bogus.DataSets.Name.Gender.Female;
            var maleFirstName = f.Name.FirstName(Bogus.DataSets.Name.Gender.Male);
            return $"{f.Name.LastName(gender)} {f.Name.FirstName(gender)} {GeneratePatronymic(maleFirstName, gender)}";
        })
        .RuleFor(c => c.StartDate, f =>
            DateOnly.FromDateTime(f.Date.Between(DateTime.Today, DateTime.Today.AddMonths(3))))
        .RuleFor(c => c.EndDate, (f, c) =>
            c.StartDate.AddDays(f.Random.Int(30, 180)))
        .RuleFor(c => c.MaxStudents, f => f.Random.Int(10, 100))
        .RuleFor(c => c.CurrentStudents, (f, c) => f.Random.Int(0, c.MaxStudents))
        .RuleFor(c => c.HasCertificate, f => f.Random.Bool())
        .RuleFor(c => c.Price, f => Math.Round(f.Random.Decimal(5000m, 150000m), 2))
        .RuleFor(c => c.Rating, f => f.Random.Int(1, 5));

    /// <summary>
    /// Генерирует учебный курс с указанным идентификатором.
    /// </summary>
    /// <param name="id">Идентификатор курса.</param>
    public static Course Generate(int id)
    {
        Faker.UseSeed(id);
        var course = Faker.Generate();
        course.Id = id;
        return course;
    }

    /// <summary>
    /// Генерирует отчество преподавателя по мужскому имени и полу.
    /// </summary>
    /// <param name="maleFirstName">Мужское имя.</param>
    /// <param name="gender">Пол преподавателя.</param>
    private static string GeneratePatronymic(string maleFirstName, Bogus.DataSets.Name.Gender gender)
    {
        var isMale = gender == Bogus.DataSets.Name.Gender.Male;

        if (maleFirstName.EndsWith('ь') || maleFirstName.EndsWith('й'))
            return maleFirstName[..^1] + (isMale ? "евич" : "евна");

        if (maleFirstName.EndsWith('а') || maleFirstName.EndsWith('я'))
            return maleFirstName[..^1] + (isMale ? "ич" : "ична");

        return maleFirstName + (isMale ? "ович" : "овна");
    }
}
