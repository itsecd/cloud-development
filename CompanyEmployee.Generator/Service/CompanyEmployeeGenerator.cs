using Bogus;
using Bogus.DataSets;
using CompanyEmployee.Generator.Dto;

namespace CompanyEmployee.Generator.Service;

/// <summary>
/// Генератор сотрудника по идентификатору
/// </summary>
/// <param name="logger">Логгер</param>
public class CompanyEmployeeGenerator(
    ILogger<CompanyEmployeeGenerator> logger
    ) : ICompanyEmployeeGenerator
{
    private static readonly string[] _position = ["Developer", "Manager", "Analyst", "QA"];
    
    private static readonly string[] _grade = ["Junior", "Middle", "Senior"];
    
    public CompanyEmployeeDto Generate(int employeeId)
    {
        var faker = new Faker<CompanyEmployeeDto>("ru")
            .RuleFor(e => e.Id, _ => employeeId)
            .RuleFor(e => e.FullName, f =>
            {
                var gender = f.PickRandom(Enum.GetValues(typeof(Name.Gender)).Cast<Name.Gender>().ToArray());

                return $"{f.Name.LastName(gender)} {f.Name.FirstName(gender)} " +
                       $"{f.Name.FirstName(gender)}{(gender == Name.Gender.Male ? "еевич" : "еевна")}";
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
            .RuleFor(e => e.DismissalFlag, f => f.Random.Bool(0.5f))
            .RuleFor(e => e.DismissalDate, (f, e) => !e.DismissalFlag ? null : 
                    f.Date.BetweenDateOnly(e.EmploymentDate, DateOnly.FromDateTime(DateTime.UtcNow)));
        
         logger.LogInformation("Generated employee with id {employeeId}", employeeId);
        return faker.Generate();
    }
}