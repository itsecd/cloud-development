using Bogus;
using CreditApp.Domain.Data;

namespace CreditApp.Api.Services;

/// <summary>
/// Генератор тестовых данных для кредитных заявок.
/// </summary>
public static class CreditGenerator
{
    private const double CbRate = 16.0;

    private static readonly string[] _statuses =
    {
        "Новая",
        "В обработке",
        "Одобрена",
        "Отклонена"
    };

    private static readonly string[] _types =
    {
        "Потребительский",
        "Ипотека",
        "Автокредит"
    };

    private static readonly Faker<CreditApplication> _faker =
        new Faker<CreditApplication>()
            .RuleFor(x => x.Id, f => f.IndexFaker)
            .RuleFor(x => x.CreditType, f => f.PickRandom(_types))
            .RuleFor(x => x.RequestedAmount,
                f => Math.Round(f.Random.Decimal(10_000, 5_000_000), 2))
            .RuleFor(x => x.TermMonths,
                f => f.Random.Int(6, 360))
            .RuleFor(x => x.InterestRate,
                f => Math.Round(f.Random.Double(CbRate, CbRate + 5), 2))
            .RuleFor(x => x.ApplicationDate,
                f => DateOnly.FromDateTime(f.Date.Past(2)))
            .RuleFor(x => x.HasInsurance,
                f => f.Random.Bool())
            .RuleFor(x => x.Status,
                f => f.PickRandom(_statuses))
            .RuleFor(x => x.DecisionDate, (f, x) =>
                x.Status is "Одобрена" or "Отклонена"
                    ? DateOnly.FromDateTime(
                        f.Date.Between(
                            x.ApplicationDate.ToDateTime(TimeOnly.MinValue),
                            DateTime.Now))
                    : null)
            .RuleFor(x => x.ApprovedAmount, (f, x) =>
                x.Status == "Одобрена"
                    ? Math.Round(f.Random.Decimal(10_000, x.RequestedAmount), 2)
                    : null);

    public static CreditApplication Generate(int id)
    {
        var result = _faker.Generate();
        result.Id = id;
        return result;
    }
}