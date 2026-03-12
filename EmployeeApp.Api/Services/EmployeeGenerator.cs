using Bogus;
using Bogus.DataSets;
using EmployeeApp.Api.Entities;

namespace EmployeeApp.Api.Services;

/// <summary>
/// Генератор данных сотрудников на основе Bogus
/// </summary>
public static class EmployeeGenerator
{
    private static readonly string[] _positions =
        ["Developer", "Manager", "Analyst", "Designer", "Tester", "DevOps Engineer", "Architect", "Consultant"];

    private static readonly string[] _suffixes =
        ["Junior", "Middle", "Senior", "Lead", "Principal"];

    private static readonly Dictionary<string, (decimal Min, decimal Max)> _salaryRanges = new()
    {
        ["Junior"] = (30_000m, 100_000m),
        ["Middle"] = (100_000m, 250_000m),
        ["Senior"] = (250_000m, 300_000m),
        ["Lead"] = (300_000m, 500_000m),
        ["Principal"] = (180_000m, 250_000m)
    };

    /// <summary>
    /// Генерация отчества на основе имени и пола
    /// </summary>
    private static string GeneratePatronymic(Faker f, Name.Gender gender)
    {
        var fatherName = f.Name.FirstName(Name.Gender.Male);
        return gender == Name.Gender.Male
            ? fatherName + "ович"
            : fatherName + "овна";
    }

    /// <summary>
    /// Генерация фамилии с учётом пола
    /// </summary>
    private static string GenerateLastName(Faker f, Name.Gender gender)
    {
        var lastName = f.Name.LastName(Name.Gender.Male);
        return gender == Name.Gender.Female
            ? lastName + "а"
            : lastName;
    }

    private static readonly Faker<Employee> _faker = new Faker<Employee>("ru")
        .RuleFor(e => e.FullName, f =>
        {
            var gender = f.PickRandom<Name.Gender>();
            var lastName = GenerateLastName(f, gender);
            var firstName = f.Name.FirstName(gender);
            var patronymic = GeneratePatronymic(f, gender);
            return $"{lastName} {firstName} {patronymic}";
        })
        .RuleFor(e => e.Position, (f, _) =>
        {
            var suffix = f.PickRandom(_suffixes);
            var position = f.PickRandom(_positions);
            return $"{suffix} {position}";
        })
        .RuleFor(e => e.Department, f => f.Commerce.Department())
        .RuleFor(e => e.HireDate, f =>
            DateOnly.FromDateTime(f.Date.Past(10)))
        .RuleFor(e => e.Salary, (f, e) =>
        {
            var suffix = e.Position.Split(' ')[0];
            var (min, max) = _salaryRanges.GetValueOrDefault(suffix, (30_000m, 60_000m));
            return Math.Round(f.Random.Decimal(min, max), 2);
        })
        .RuleFor(e => e.Email, f => f.Internet.Email())
        .RuleFor(e => e.PhoneNumber, f =>
            f.Phone.PhoneNumber("+7(###)###-##-##"))
        .RuleFor(e => e.IsDismissed, f => f.Random.Bool(0.2f))
        .RuleFor(e => e.DismissalDate, (f, e) =>
            e.IsDismissed
                ? DateOnly.FromDateTime(f.Date.Between(e.HireDate.ToDateTime(TimeOnly.MinValue), DateTime.Now))
                : null);

    /// <summary>
    /// Генерация сотрудника по идентификатору
    /// </summary>
    public static Employee Generate(int id)
    {
        var employee = _faker.Generate();
        employee.Id = id;
        return employee;
    }
}
