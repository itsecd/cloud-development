using Bogus;
using Bogus.DataSets;
using Service.Api.Entities;

namespace Service.Api.Generator;

/// <summary>
/// Генератор случайных сотрудников компании
/// </summary>
public static class EmployeeGenerator
{
    private static readonly Faker<Employee> _faker;

    /// <summary>
    /// Справочник профессий
    /// </summary>
    private static readonly string[] _professions =
    {
        "Developer",
        "Manager",
        "Analyst",
        "Tester",
        "Administrator",
        "Designer"
    };

    /// <summary>
    /// Справочник суффиксов
    /// </summary>
    private static readonly string[] _levels =
    {
        "Junior",
        "Middle",
        "Senior"
    };

    static EmployeeGenerator()
    {
        _faker = new Faker<Employee>("ru")


            // ФИО
            .RuleFor(e => e.FullName, f =>
            {
                var gender = f.PickRandom<Name.Gender>();

                var firstName = f.Name.FirstName(gender);
                var lastName = f.Name.LastName(gender);

                var patronymicSuffix = gender == Name.Gender.Male ? "ович" : "овна";
                var patronymic = f.Name.FirstName(Name.Gender.Male) + patronymicSuffix;

                return $"{lastName} {firstName} {patronymic}";
            })

            // Должность
            .RuleFor(e => e.Position, f =>
            {
                var level = f.PickRandom(_levels);
                var profession = f.PickRandom(_professions);

                return $"{level} {profession}";
            })

            // Отдел
            .RuleFor(e => e.Department, f => f.Commerce.Department())

            // Дата приема (не более 10 лет назад)
            .RuleFor(e => e.HireDate,
                f => DateOnly.FromDateTime(f.Date.Past(10)))

            // Оклад (зависит от уровня)
            .RuleFor(e => e.Salary, (f, e) =>
            {
                if (e.Position!.Contains("Junior"))
                    return Math.Round(f.Random.Decimal(50000, 90000), 2);

                if (e.Position.Contains("Middle"))
                    return Math.Round(f.Random.Decimal(90000, 150000), 2);

                return Math.Round(f.Random.Decimal(150000, 250000), 2);
            })

            // Email
            .RuleFor(e => e.Email, f => f.Internet.Email())

            // Телефон
            .RuleFor(e => e.Phone,
                f => f.Phone.PhoneNumber("+7(###)###-##-##"))

            // Индикатор увольнения
            .RuleFor(e => e.IsFired,
                f => f.Random.Bool(0.2f)) // 20% сотрудников уволены

            // Дата увольнения
            .RuleFor(e => e.FireDate, (f, e) =>
            {
                if (!e.IsFired)
                    return null;

                return f.Date.BetweenDateOnly(e.HireDate, DateOnly.FromDateTime(DateTime.Now));
            });
    }

    /// <summary>
    /// Генерирует одного случайного сотрудника
    /// </summary>
    public static Employee Generate(int id)
    {
        var employee = _faker.Generate();
        employee.Id = id;
        return employee;
    }
}