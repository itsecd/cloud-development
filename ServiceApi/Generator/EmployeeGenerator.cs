using Bogus;
using Bogus.DataSets;
using Service.Api.Entities;

namespace Service.Api.Generator;

/// <summary>
/// Генератор случайных сотрудников компании.
/// </summary>
public static class EmployeeGenerator
{
    private static readonly string[] ProfessionCatalog =
    {
        "Developer",
        "Manager",
        "Analyst",
        "Tester",
        "Administrator",
        "Designer"
    };

    private static readonly string[] PositionLevels =
    {
        "Junior",
        "Middle",
        "Senior"
    };

    public static Employee Generate(int id)
    {
        var faker = new Faker("ru");

        var gender = faker.PickRandom<Name.Gender>();
        var firstName = faker.Name.FirstName(gender);
        var lastName = faker.Name.LastName(gender);
        var patronymic = BuildPatronymic(faker.Name.FirstName(Name.Gender.Male), gender);

        var level = faker.PickRandom(PositionLevels);
        var profession = faker.PickRandom(ProfessionCatalog);
        var hireDate = faker.Date.Past(10, DateTime.Today);
        var isFired = faker.Random.Bool(0.18f);

        DateOnly? fireDate = null;
        if (isFired)
        {
            fireDate = DateOnly.FromDateTime(faker.Date.Between(hireDate, DateTime.Today));
        }

        return new Employee
        {
            Id = id,
            FullName = $"{lastName} {firstName} {patronymic}",
            Position = $"{level} {profession}",
            Department = faker.Commerce.Department(),
            HireDate = DateOnly.FromDateTime(hireDate),
            Salary = CalculateSalary(level, faker),
            Email = faker.Internet.Email(firstName, lastName),
            Phone = faker.Phone.PhoneNumber("+7(###)###-##-##"),
            IsFired = isFired,
            FireDate = fireDate
        };
    }

    private static string BuildPatronymic(string sourceName, Name.Gender gender)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
        {
            return gender == Name.Gender.Male ? "Иванович" : "Ивановна";
        }

        return gender switch
        {
            Name.Gender.Male when sourceName.EndsWith('й') => $"{sourceName[..^1]}евич",
            Name.Gender.Female when sourceName.EndsWith('й') => $"{sourceName[..^1]}евна",
            Name.Gender.Male => $"{sourceName}ович",
            _ => $"{sourceName}овна"
        };
    }

    private static decimal CalculateSalary(string level, Faker faker)
    {
        var value = level switch
        {
            "Junior" => faker.Random.Decimal(60_000m, 95_000m),
            "Middle" => faker.Random.Decimal(100_000m, 170_000m),
            "Senior" => faker.Random.Decimal(180_000m, 280_000m),
            _ => faker.Random.Decimal(80_000m, 120_000m)
        };

        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
