using Bogus;
using Generator.Dto;

namespace Generator.Services;

/// <summary>
/// Генератор псевдослучайных кредитных заявок для демо/тестирования.
/// Включает простые зависимости между статусом и полями решения.
/// </summary>
public class CreditOrderGenerator
{
    private static readonly string[] _сreditTypes = { "Потребительский", "Ипотека", "Автокредит", "Микрозайм" };
    private static readonly string[] _statuses = { "Новая", "В обработке", "Одобрена", "Отклонена" };

    /// <summary>
    /// Генерирует кредитную заявку для заданного <paramref name="id"/> с реалистичными полями.
    /// </summary>
    /// <param name="id">Идентификатор заявки.</param>
    /// <returns>Сгенерированная заявка.</returns>
    public CreditOrderDto Generate(int id)
    {        
        var faker = new Faker<CreditOrderDto>("ru")
            .RuleFor(x => x.Id, _ => id)
            .RuleFor(x => x.CreditType, f => f.PickRandom(_сreditTypes))
            .RuleFor(x => x.RequestedSum, f => Math.Round(f.Finance.Amount(1_000_000m, 100_000_000m), 2))
            .RuleFor(x => x.MonthsDuration, f => f.Random.Int(1, 360))
            .RuleFor(x => x.InterestRate, f => Math.Round(f.Random.Double(15.6, 20.0), 2))
            .RuleFor(x => x.FilingDate, f => DateOnly.FromDateTime(f.Date.Past(2)))
            .RuleFor(x => x.IsInsuranceNeeded, f => f.Random.Bool())
            .RuleFor(x => x.OrderStatus, f => f.PickRandom(_statuses))
            .RuleFor(x => x.DecisionDate, _ => null)
            .RuleFor(x => x.ApprovedSum, _ => null)
            .RuleFor(x => x.DecisionDate, (f, o) =>
                o.OrderStatus is "Одобрена" or "Отклонена"
                    ? o.FilingDate.AddDays(f.Random.Int(0, 31))
                    : null)
            .RuleFor(x => x.ApprovedSum, (f, o) =>
            {
                if (o.OrderStatus is not "Одобрена")
                    return null;

                var k = f.Random.Double(0.6, 1.0);
                var approved = o.RequestedSum * (decimal)k;
                return Math.Round(approved, 2);
            });

        return faker.Generate();
    }
}
