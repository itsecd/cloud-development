using Bogus;
using Bogus.DataSets;
using CompanyEmployee.ApiService.Models;

namespace CompanyEmployee.ApiService.Services;

/// <summary>
/// Генератор сотрудника
/// </summary>
public class CompanyEmployeeGenerator
{

    /// <summary>
    /// Справочник профессий сотрудников
    /// </summary>
    private static readonly string[] _professionTypes =
    [
        "Developer",
        "Manager",
        "Analyst"
    ];

    /// <summary>
    /// Справочник суффиксов для профессий
    /// </summary>
    private static readonly string[] _suffix =
    [
        "Junior",
        "Middle",
        "Senior"
    ];

    /// <summary>
    /// Генерация должности
    /// </summary>
    private static string GenerateJobTitle(Faker f)
    {
        var suffix = f.PickRandom(_suffix);
        var profession = f.PickRandom(_professionTypes);

        return $"{suffix} {profession}";
    }

    /// <summary>
    /// Генерация зарплаты
    /// </summary>
    private static decimal GenerateSalary(Faker f, string jobTitle)
    {
        var level = jobTitle.Split(' ')[0];

        var (min, max) = level switch
        {
            "Junior" => (50000m, 90000m),
            "Middle" => (90000m, 150000m),
            "Senior" => (150000m, 250000m),
            _ => (50000m, 250000m)
        };

        return Math.Round(f.Random.Decimal(min, max), 2);
    }

    /// <summary>
    /// Генерация полного имени
    /// </summary>
    private static string GenerateFullName(Faker f)
    {
        var gender = f.PickRandom<Bogus.DataSets.Name.Gender>();

        var firstName = f.Name.FirstName(gender);
        var lastName = f.Name.LastName(gender);

        var fatherName = f.Name.FirstName(Bogus.DataSets.Name.Gender.Male);

        string patronymic;

        if (fatherName.EndsWith("й") || fatherName.EndsWith("ь"))
        {
            patronymic = fatherName[..^1] + (gender == Name.Gender.Male ? "евич" : "евна");
        }
        else
        {
            patronymic = fatherName + (gender == Name.Gender.Male ? "ович" : "овна");
        }

        return $"{lastName} {firstName} {patronymic}";
    }

    /// <summary>
    /// Генерация даты уволнения
    /// </summary>
    private static DateOnly? GenerateDismissalDate(Faker f, bool Dismissal, DateOnly AdmissionDate)
    {
        return Dismissal ? f.Date.BetweenDateOnly(AdmissionDate, DateOnly.FromDateTime(DateTime.UtcNow)) : null;
    }

    /// <summary>
    /// Преднастроенный генератор
    /// </summary>
    private static readonly Faker<CompanyEmployeeModel> _faker = new Faker<CompanyEmployeeModel>("ru")
        .RuleFor(e => e.FullName, f => GenerateFullName(f))
        .RuleFor(e => e.JobTitle, f => GenerateJobTitle(f))
        .RuleFor(e => e.Department, f => f.Commerce.Department())
        .RuleFor(e => e.AdmissionDate, f => f.Date.PastDateOnly(10))
        .RuleFor(e => e.Salary, (f, e) => GenerateSalary(f, e.JobTitle))
        .RuleFor(e => e.Email, (f, e) => f.Internet.Email())
        .RuleFor(x => x.PhoneNumber, f => f.Phone.PhoneNumber("+7(###)###-##-##"))
        .RuleFor(x => x.Dismissal, f => f.Random.Bool())
        .RuleFor(x => x.DismissalDate, (f, e) => GenerateDismissalDate(f, e.Dismissal, e.AdmissionDate));

    /// <summary>
    /// Генерация сотрудника
    /// </summary>
    public CompanyEmployeeModel Generate(int id)
    {
        var emploee = _faker.Generate();
        emploee.Id = id;
        return emploee;
    }
}
