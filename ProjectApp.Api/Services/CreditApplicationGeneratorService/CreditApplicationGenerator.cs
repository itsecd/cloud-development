using Bogus;
using ProjectApp.Domain.Entities;

namespace ProjectApp.Api.Services.CreditApplicationGeneratorService;

/// <summary>
/// Генератор случайных кредитных заявок с использованием Bogus
/// </summary>
public class CreditApplicationGenerator
{
    private static readonly string[] CreditTypes =
    {
        "Потребительский", "Ипотека", "Автокредит", "Рефинансирование",
        "Образовательный", "Кредитная карта", "Бизнес"
    };

    private static readonly string[] Statuses = { "Новая", "В обработке", "Одобрена", "Отклонена" };

    private readonly Faker<CreditApplication> _faker;
    private readonly double _minInterestRatePercent;

    public CreditApplicationGenerator(IConfiguration configuration, ILogger<CreditApplicationGenerator> logger)
    {
        _minInterestRatePercent = configuration.GetValue("FinanceSettings:MinInterestRatePercent", 16.0);
        logger.LogInformation("Minimum interest rate set to {Rate}%", _minInterestRatePercent);

        _faker = new Faker<CreditApplication>("ru")
            .RuleFor(c => c.Id, f => f.IndexFaker + 1)
            .RuleFor(c => c.CreditType, f => f.PickRandom(CreditTypes))
            .RuleFor(c => c.RequestedAmount, f => Math.Round(f.Finance.Amount(50_000, 10_000_000), 2))
            .RuleFor(c => c.TermMonths, f => f.Random.Int(6, 360))
            .RuleFor(c => c.InterestRate, f =>
            {
                var rate = f.Random.Double(_minInterestRatePercent, _minInterestRatePercent + 15.0);
                return Math.Round(rate, 2);
            })
            .RuleFor(c => c.ApplicationDate, f =>
            {
                var date = f.Date.PastDateOnly(2);
                var today = DateOnly.FromDateTime(DateTime.Now);
                return date > today ? today : date;
            })
            .RuleFor(c => c.RequiresInsurance, f => f.Random.Bool())
            .RuleFor(c => c.Status, f => f.PickRandom(Statuses))
            .RuleFor(c => c.DecisionDate, (f, c) =>
            {
                if (c.Status is "Одобрена" or "Отклонена")
                {
                    var start = c.ApplicationDate.ToDateTime(TimeOnly.MinValue).AddDays(1);
                    var end = DateTime.Now;
                    if (start >= end)
                    {
                        return DateOnly.FromDateTime(start);
                    }

                    var decided = f.Date.Between(start, end);
                    return DateOnly.FromDateTime(decided);
                }

                return null;
            })
            .RuleFor(c => c.ApprovedAmount, (f, c) =>
            {
                if (c.Status == "Одобрена")
                {
                    var factor = f.Random.Decimal(0.5m, 1.0m);
                    var amount = Math.Round(c.RequestedAmount * factor, 2);
                    return amount <= c.RequestedAmount ? amount : c.RequestedAmount;
                }

                return null;
            });
    }

    /// <summary>
    /// Генерирует одну случайную кредитную заявку
    /// </summary>
    public CreditApplication Generate() => _faker.Generate();
}
