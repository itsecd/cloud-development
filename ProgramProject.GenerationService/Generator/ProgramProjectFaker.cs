using Bogus;
using ProgramProject.GenerationService.Models;

namespace ProgramProject.GenerationService.Generator;

public class ProgramProjectFaker
{
    private readonly Faker<ProgramProjectModel> _faker;

    public ProgramProjectFaker()
    {
        _faker = new Faker<ProgramProjectModel>("ru")
            .RuleFor(p => p.Id, f => f.IndexVariable++)

            /// <summary>
            /// Название проекта: комбинация из Commerce, Hacker, Finance, Lorem
            /// </summary>
            .RuleFor(p => p.Name, f =>
                f.PickRandom(new[]
                {
                    f.Commerce.ProductName() + " " + f.Hacker.Abbreviation(),
                    "Project " + f.Hacker.Noun(),
                    f.Finance.AccountName() + " System",
                    f.Lorem.Word() + "-" + f.Lorem.Word()
                }))

            /// <summary>
            /// Заказчик — компания
            /// </summary>
            .RuleFor(p => p.Customer, f => f.Company.CompanyName())

            /// <summary>
            /// Менеджер проекта — полное имя
            /// </summary>
            .RuleFor(p => p.Manager, f => f.Name.FullName())

            /// <summary>
            /// Дата начала
            /// </summary>
            .RuleFor(p => p.StartDate, f =>
            {
                var start = f.Date.Past(5);
                return DateOnly.FromDateTime(start);
            })

            /// <summary>
            /// Плановая дата завершения: позже даты начала
            /// </summary>
            .RuleFor(p => p.PlannedEndDate, (f, p) =>
            {
                var planned = f.Date.Soon(180, p.StartDate.ToDateTime(TimeOnly.MinValue));
                return DateOnly.FromDateTime(planned);
            })

            /// <summary>
            /// Бюджет: от 10k до 1M
            /// </summary>
            .RuleFor(p => p.Budget, f => f.Finance.Amount(10000, 1000000, 2))

            /// <summary>
            /// Процент выполнения: от 0 до 100
            /// </summary>
            .RuleFor(p => p.CompletionPercentage, f => f.Random.Int(0, 100))

            /// <summary>
            /// Фактические затраты: пропорциональны бюджету (50-120%)
            /// </summary>
            .RuleFor(p => p.ActualCost, (f, p) =>
                f.Finance.Amount(p.Budget * 0.5m, p.Budget * 1.2m, 2))

            /// <summary>
            /// Фактическая дата завершения: заполняется только если процент = 100
            /// </summary>
            .RuleFor(p => p.ActualEndDate, (f, p) =>
            {
                if (p.CompletionPercentage == 100)
                {
                    var actual = f.Date.Recent(30);
                    return DateOnly.FromDateTime(actual);
                }
                return null;
            })

            /// <summary>
            /// Финальная корректировка: если есть ActualEndDate, процент должен быть 100
            /// </summary>
            .FinishWith((f, p) =>
            {
                if (p.ActualEndDate.HasValue)
                {
                    p.CompletionPercentage = 100;
                }
            });
    }

    public ProgramProjectModel Generate()
    {
        return _faker.Generate();
    }

    public List<ProgramProjectModel> Generate(int count)
    {
        return _faker.Generate(count);
    }
}
