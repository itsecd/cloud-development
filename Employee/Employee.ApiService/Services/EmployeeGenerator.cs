using Bogus;
using Employee.ApiService.Models;

namespace Employee.ApiService.Services;

/// <summary>
/// Генератор тестовых сотрудников
/// </summary>
public class EmployeeGenerator
{
    /// <summary>
    /// Справочник профессий
    /// </summary>
    private static readonly string[] _professions =
    [
        "Developer",
        "Manager",
        "Analyst",
        "QA",
        "DevOps",
        "Designer"
    ];

    /// <summary>
    /// Справочник суффиксов должностей и коэффициентов зарплаты
    /// </summary>
    private static readonly Dictionary<string, decimal> _positionLevels = new()
    {
        { "Junior", 0.7m },
        { "Middle", 1.0m },
        { "Senior", 1.5m },
        { "Lead", 2.0m }
    };

    /// <summary>
    /// Константа базовой зарплаты
    /// </summary>
    private const decimal BaseSalary = 100000m;

    /// <summary>
    /// Генерация должности
    /// </summary>
    private static string GeneratePosition(Faker f)
    {
        var level = f.PickRandom(_positionLevels.Keys.ToArray());
        var profession = f.PickRandom(_professions);

        return $"{level} {profession}";
    }

    /// <summary>
    /// Генерация даты приема
    /// </summary>
    private static DateOnly GenerateAdmissionDate(Faker f)
    {
        return DateOnly.FromDateTime(f.Date.Past(10));
    }

    /// <summary>
    /// Генерация зарплаты с учетом коэффициента уровня
    /// </summary>
    private static decimal GenerateSalary(Faker f, string position)
    {
        var level = _positionLevels.Keys.FirstOrDefault(position.Contains);

        decimal coefficient = 1;

        if (level != null)
        {
            coefficient = _positionLevels[level];
        }

        var randomFactor = f.Random.Decimal(0.9m, 1.1m);

        var salary = BaseSalary * coefficient * randomFactor;

        return Math.Round(salary, 2);
    }

    /// <summary>
    /// Генерация даты увольнения
    /// </summary>
    private static DateOnly? GenerateDismissalDate(Faker f, EmployeeModel employee)
    {
        if (!employee.DismissalIndicator)
            return null;

        var start = employee.DateAdmission.ToDateTime(TimeOnly.MinValue);

        var dismissal = f.Date.Between(start, DateTime.Now);

        return DateOnly.FromDateTime(dismissal);
    }
    /// <summary>
    /// Генерация ФИО
    /// </summary>
    private static string GenerateFullName(Faker f)
    {
        var gender = f.PickRandom<Bogus.DataSets.Name.Gender>();

        var firstName = f.Name.FirstName(gender);
        var lastName = f.Name.LastName(gender);

        // имя отца
        var fatherName = f.Name.FirstName(Bogus.DataSets.Name.Gender.Male);

        var patronymic = gender == Bogus.DataSets.Name.Gender.Male
            ? fatherName + "ович"
            : fatherName + "овна";

        return $"{lastName} {firstName} {patronymic}";
    }

    /// <summary>
    /// Преднастроенный генератор
    /// </summary>
    private static readonly Faker<EmployeeModel> _faker = new Faker<EmployeeModel>("ru")
        .RuleFor(e => e.Name, f => GenerateFullName(f))
        .RuleFor(e => e.Position, f => GeneratePosition(f))
        .RuleFor(e => e.Department, f => f.Commerce.Department())
        .RuleFor(e => e.DateAdmission, f => GenerateAdmissionDate(f))
        .RuleFor(e => e.Salary, (f, e) => GenerateSalary(f, e.Position))
        .RuleFor(e => e.Email, (f, e) => f.Internet.Email())
        .RuleFor(e => e.Phone, f => f.Phone.PhoneNumber("+7(###)###-##-##"))
        .RuleFor(e => e.DismissalIndicator, f => f.Random.Bool(0.2f))
        .RuleFor(e => e.DateDismissal, (f, e) => GenerateDismissalDate(f, e));

    /// <summary>
    /// Генерация сотрудника
    /// </summary>
    public EmployeeModel Generate(int id)
    {
        var employee = _faker.Generate();
        employee.Id = id;

        return employee;
    }

}
