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
    private static readonly object FakerLock = new();

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

    private static readonly Faker<CourseContract> ContractFaker = new Faker<CourseContract>("ru")
        .RuleFor(contract => contract.Id, _ => 0)
        .RuleFor(contract => contract.CourseName, f => f.PickRandom(CourseDictionary))
        .RuleFor(contract => contract.TeacherFullName, f =>
        {
            var gender = f.PickRandom<Name.Gender>(Name.Gender.Male, Name.Gender.Female);
            var firstName = f.Name.FirstName(gender);
            var lastName = f.Name.LastName(gender);
            var patronymic = gender == Name.Gender.Male
                ? f.PickRandom(MalePatronymicDictionary)
                : f.PickRandom(FemalePatronymicDictionary);

            return $"{lastName} {firstName} {patronymic}";
        })
        .RuleFor(contract => contract.StartDate, f => DateOnly.FromDateTime(f.Date.Soon(60)))
        .RuleFor(contract => contract.EndDate, (f, contract) => contract.StartDate.AddDays(f.Random.Int(1, 180)))
        .RuleFor(contract => contract.MaxStudents, f => f.Random.Int(10, 200))
        .RuleFor(contract => contract.CurrentStudents, (f, contract) => f.Random.Int(0, contract.MaxStudents))
        .RuleFor(contract => contract.HasCertificate, f => f.Random.Bool())
        .RuleFor(contract => contract.Price, f => decimal.Round(f.Random.Decimal(1000m, 120000m), 2, MidpointRounding.AwayFromZero))
        .RuleFor(contract => contract.Rating, f => f.Random.Int(1, 5));

    /// <inheritdoc />
    public IReadOnlyList<CourseContract> Generate(int count)
    {
        logger.LogInformation("Course generation started: {Count}", count);

        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");
        }

        List<CourseContract> generatedContracts;
        lock (FakerLock)
        {
            generatedContracts = ContractFaker.Generate(count);
        }

        var courses = generatedContracts
            .Select((contract, index) => contract with { Id = index + 1 })
            .ToList();
        logger.LogInformation("Course generation completed: {Count}", courses.Count);

        return courses;
    }

    /// <inheritdoc />
    public CourseContract GenerateById(int id)
    {
        if (id < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Id must be non-negative.");
        }

        CourseContract contract;
        lock (FakerLock)
        {
            contract = ContractFaker
                .UseSeed(id + 1)
                .Generate() with { Id = id };
        }

        return contract;
    }
}