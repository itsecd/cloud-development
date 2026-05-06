using Bogus;
using Bogus.DataSets;
using Cloud.API.Models;
using Cloud.API.Services;

namespace Cloud.Api.Services;

/// <summary>
/// Генератор сотрудника по заданному id
/// </summary>
/// <param name="logger">Логгер</param>
public class EmployeeGenerator(
    ILogger<EmployeeGenerator> logger
    ) : IEmployeeGenerator
{
    private readonly Faker<Employee> _faker = new Faker<Employee>("ru")
        .RuleFor(e => e.Id, _ => 0)
        .RuleFor(e => e.FullName, f =>
        {
            var gender = f.PickRandom<Name.Gender>();
            var firstName = f.Name.FirstName(gender);
            var lastName = f.Name.LastName(gender);
            var patronymic = $"{f.Name.FirstName(Name.Gender.Male)}{(gender == Name.Gender.Male ? "ович" : "овна")}";
            return $"{lastName} {firstName} {patronymic}";
        })
        .RuleFor(e => e.Position, f =>
        {
            var professions = new[] { "Developer", "Manager", "Analyst", "Designer", "QA" };
            var suffixes = new[] { "Junior", "Middle", "Senior", "Lead" };
            var suffix = f.PickRandom(suffixes);
            var profession = f.PickRandom(professions);
            return $"{suffix} {profession}";
        })
        .RuleFor(e => e.Department, f => f.Commerce.Department())
        .RuleFor(e => e.HireDate, f => DateOnly.FromDateTime(f.Date.Past(10)))
        .RuleFor(e => e.Salary, (f, e) =>
        {
            var suffix = e.Position.Split(' ')[0];
            decimal baseSalary = suffix switch
            {
                "Junior" => 50000,
                "Middle" => 100000,
                "Senior" => 150000,
                "Lead" => 200000,
                _ => 70000
            };
            return Math.Round(baseSalary + f.Random.Decimal(-5000, 25000), 2);
        })
        .RuleFor(e => e.Email, (f, e) => f.Internet.Email(e.FullName))
        .RuleFor(e => e.PhoneNumber, f => f.Phone.PhoneNumber("+7(###)###-##-##"))
        .RuleFor(e => e.IsFired, f => f.Random.Bool(0.2f))
        .RuleFor(e => e.FiredDate, (f, e) =>
            e.IsFired ? DateOnly.FromDateTime(f.Date.Between(e.HireDate.ToDateTime(TimeOnly.MinValue), DateTime.Now)) : null);

    /// <inheritdoc />
    public Employee Generate(int id)
    {
        var employee = _faker.Generate();
        employee.Id = id;
        logger.LogInformation("Generated employee with id {employeeId}", id);
        return employee;
    }
}