using Bogus;
using ProgramProject.GenerationService.Models;

namespace ProgramProject.GenerationService.Generator;

/// <summary>
/// Генератор нового проекта
/// </summary>
public class ProgramProjectFaker : IProgramProjectFaker
{
    private readonly Faker<ProgramProjectModel> _faker;

    public ProgramProjectFaker()
    {
        _faker = new Faker<ProgramProjectModel>("ru")
            .RuleFor(p => p.Id, f => f.IndexVariable++)

            // Название проекта: комбинация из Commerce, Hacker, Finance, Lorem
            .RuleFor(p => p.Name, f =>
                f.PickRandom(
                    f.Commerce.ProductName() + " " + f.Hacker.Abbreviation(),
                    "Project " + f.Hacker.Noun(),
                    f.Finance.AccountName() + " System",
                    f.Lorem.Word() + "-" + f.Lorem.Word()
                ))

            // Заказчик — компания
            .RuleFor(p => p.Customer, f => f.Company.CompanyName())

            // Менеджер проекта — полное имя
            .RuleFor(p => p.Manager, f => f.Name.FullName())

            // Дата начала
            .RuleFor(p => p.StartDate, f =>
            {
                var start = f.Date.Past(5);
                return DateOnly.FromDateTime(start);
            })

            // Плановая дата завершения: позже даты начала
            .RuleFor(p => p.PlannedEndDate, (f, p) =>
            {
                var planned = f.Date.Soon(180, p.StartDate.ToDateTime(TimeOnly.MinValue));
                return DateOnly.FromDateTime(planned);
            })

            // Бюджет: от 10k до 1M
            .RuleFor(p => p.Budget, f => f.Finance.Amount(10000, 1000000, 2))

            // Процент выполнения: от 0 до 100
            .RuleFor(p => p.CompletionPercentage, f => f.Random.Int(0, 100))

            // Фактические затраты: пропорциональны бюджету (50-120%)
            .RuleFor(p => p.ActualCost, (f, p) =>
                f.Finance.Amount(p.Budget * 0.5m, p.Budget * 1.2m, 2))

            // Фактическая дата завершения: заполняется только если процент = 100
            .RuleFor(p => p.ActualEndDate, (f, p) =>
            {
                if (p.CompletionPercentage == 100)
                {
                    var actual = f.Date.Recent(30);
                    return DateOnly.FromDateTime(actual);
                }
                return null;
            });
    }
    /// <summary>
    /// Метод вызова генерации
    /// </summary>
    public ProgramProjectModel Generate() =>
        _faker.Generate();
}
