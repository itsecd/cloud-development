using Bogus;
using Bogus.DataSets;
using CompanyEmployee.Generator.Dto;

namespace CompanyEmployee.Generator.Service;

/// <summary>
/// Генератор сотрудника по идентификатору
/// </summary>
public class CompanyEmployeeGenerator(
    ILogger<CompanyEmployeeGenerator> logger
    )
{
    private static readonly string[] _position = ["Developer", "Manager", "Analyst", "QA"];
    
    private static readonly string[] _grade = ["Junior", "Middle", "Senior"];
    
    private static readonly string[] _malePatronymic = 
    [
        "Александрович", "Сергеевич", "Иванович", "Дмитриевич", "Владимирович",
        "Андреевич", "Михайлович", "Николаевич", "Павлович", "Викторович"
    ];

    private static readonly string[] _femalePatronymic =
    [
        "Алексеевна", "Сергеевна", "Ивановна", "Дмитриевна", "Владимировна",
        "Андреевна", "Михайловна", "Николаевна", "Павловна", "Викторовна"
    ];

    /// <summary>
    /// Метод для генерации сотрудника по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор сотрудника</param>
    /// <returns>DTO сотрудника компании</returns>
    public CompanyEmployeeDto Generate(int id)
    {
        var faker = new Faker<CompanyEmployeeDto>("ru")
            .RuleFor(e => e.Id, _ => id)
            .RuleFor(e => e.FullName, f =>
            {
                var gender = f.PickRandom(Enum.GetValues(typeof(Name.Gender)).Cast<Name.Gender>().ToArray());

                string patronymic;
                if (gender == Name.Gender.Male)
                {
                    patronymic = f.PickRandom(_malePatronymic);
                }
                else
                {
                    patronymic = f.PickRandom(_femalePatronymic);
                }

                return $"{f.Name.LastName(gender)} {f.Name.FirstName(gender)} {patronymic}";
            })
            .RuleFor(e => e.Position, f => $"{f.PickRandom(_position)} {f.PickRandom(_grade)}")
            .RuleFor(e => e.Department, f => f.Commerce.Department())
            .RuleFor(e => e.EmploymentDate, f => f.Date.PastDateOnly(10))
            .RuleFor(e => e.Salary, (f, e) =>
            {
                var baseSalary = f.Random.Decimal(100, 200);

                for (var i = 0; i < _grade.Length; ++i)
                {
                    if (e.Position.Contains(_grade[i]))
                    {
                        baseSalary *= (i + 1);
                        break;
                    }
                }

                return Math.Round(baseSalary, 2);
            })
            .RuleFor(e => e.Email, f => f.Internet.Email())
            .RuleFor(e => e.PhoneNumber, f => f.Phone.PhoneNumber("+7(###)###-##-##"))
            .RuleFor(e => e.DismissalFlag, f => f.Finance.Random.Bool(0.5f))
            .RuleFor(e => e.DismissalDate,
                (f, e) => f.Date.BetweenDateOnly(e.EmploymentDate, DateOnly.FromDateTime(DateTime.UtcNow)));
        
        logger.LogInformation("Generated employee with id {}", id);
        return faker.Generate();
    }
}