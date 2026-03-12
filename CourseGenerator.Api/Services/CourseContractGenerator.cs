using Bogus;
using Bogus.DataSets;
using CourseGenerator.Api.Interfaces;
using CourseGenerator.Api.Models;

namespace CourseGenerator.Api.Services;

/// <summary>
/// Генератор тестовых учебных контрактов на основе Bogus.
/// </summary>
public sealed class CourseContractGenerator(ILogger<CourseContractGenerator> logger) : ICourseContractGenerator
{
    private static readonly string[] CourseDictionary =
    [
        "Основы программирования на C#",
        "Проектирование микросервисов",
        "Базы данных и SQL",
        "Инженерия требований",
        "Тестирование программного обеспечения",
        "Алгоритмы и структуры данных",
        "Распределенные системы",
        "Web-разработка на ASP.NET Core",
        "DevOps и CI/CD",
        "Машинное обучение в разработке ПО"
    ];

    private static readonly string[] MalePatronymicDictionary =
    [
        "Иванович",
        "Петрович",
        "Сергеевич",
        "Алексеевич",
        "Дмитриевич",
        "Андреевич",
        "Игоревич",
        "Олегович",
        "Владимирович",
        "Николаевич"
    ];

    private static readonly string[] FemalePatronymicDictionary =
    [
        "Ивановна",
        "Петровна",
        "Сергеевна",
        "Алексеевна",
        "Дмитриевна",
        "Андреевна",
        "Игоревна",
        "Олеговна",
        "Владимировна",
        "Николаевна"
    ];

    /// <inheritdoc />
    public IReadOnlyList<CourseContract> Generate(int count)
    {
        logger.LogInformation("Course generation started: {Count}", count);

        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");
        }

        var idSeed = 1;

        var faker = new Faker<CourseContract>("ru")
            .CustomInstantiator(f =>
            {
                var startDate = DateOnly.FromDateTime(f.Date.Soon(60));
                var endDate = startDate.AddDays(f.Random.Int(1, 180));
                var maxStudents = f.Random.Int(10, 200);
                var currentStudents = f.Random.Int(0, maxStudents);
                var price = decimal.Round(f.Random.Decimal(1000m, 120000m), 2, MidpointRounding.AwayFromZero);
                var gender = f.PickRandom<Name.Gender>(Name.Gender.Male, Name.Gender.Female);
                var firstName = f.Name.FirstName(gender);
                var lastName = f.Name.LastName(gender);
                var patronymic = gender == Name.Gender.Male
                    ? f.PickRandom(MalePatronymicDictionary)
                    : f.PickRandom(FemalePatronymicDictionary);

                return new CourseContract(
                    Id: idSeed++,
                    CourseName: f.PickRandom(CourseDictionary),
                    TeacherFullName: $"{lastName} {firstName} {patronymic}",
                    StartDate: startDate,
                    EndDate: endDate,
                    MaxStudents: maxStudents,
                    CurrentStudents: currentStudents,
                    HasCertificate: f.Random.Bool(),
                    Price: price,
                    Rating: f.Random.Int(1, 5));
            });

        var courses = faker.Generate(count);
        logger.LogInformation("Course generation completed: {Count}", courses.Count);

        return courses;
    }
}