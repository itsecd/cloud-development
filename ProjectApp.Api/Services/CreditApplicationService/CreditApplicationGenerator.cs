using Bogus;
using ProjectApp.Domain.Entities;

namespace ProjectApp.Api.Services.CreditApplicationService;

/// <summary>
/// Генератор случайных кредитных заявок с использованием Bogus
/// </summary>
public class CreditApplicationGenerator
{
    private readonly Faker<CreditApplication> _faker;

    public CreditApplicationGenerator()
    {
        _faker = new Faker<CreditApplication>("ru")
            .RuleFor(c => c.Id, f => f.IndexFaker + 1)
            .RuleFor(c => c.ClientFullName, f => f.Name.FullName())
            .RuleFor(c => c.ApplicationDate, f => f.Date.PastDateOnly(1))
            .RuleFor(c => c.CreditAmount, f => Math.Round(f.Finance.Amount(50000, 5000000), 2))
            .RuleFor(c => c.CreditTermMonths, f => f.Random.Int(6, 120))
            .RuleFor(c => c.CreditPurpose, f => f.PickRandom(
                "Покупка автомобиля",
                "Покупка недвижимости",
                "Ремонт квартиры",
                "Образование",
                "Лечение",
                "Погашение других кредитов",
                "Отпуск",
                "Покупка техники",
                "Иное"
            ))
            .RuleFor(c => c.ClientIncome, f => Math.Round(f.Finance.Amount(20000, 500000), 2))
            .RuleFor(c => c.CreditScore, f => f.Random.Int(300, 1000))
            .RuleFor(c => c.Approved, f =>
            {
                var hasDecision = f.Random.Bool(0.7f);
                if (!hasDecision)
                {
                    return null;
                }
                return f.Random.Bool(0.6f);
            })
            .RuleFor(c => c.DecisionDate, (f, c) =>
            {
                if (!c.Approved.HasValue)
                {
                    return null;
                }

                var minDate = c.ApplicationDate.ToDateTime(TimeOnly.MinValue).AddDays(1);
                var maxDate = DateTime.Now;

                if (minDate > maxDate)
                {
                    return DateOnly.FromDateTime(minDate);
                }

                var endDate = f.Date.Between(minDate, maxDate);
                return DateOnly.FromDateTime(endDate);
            })
            .RuleFor(c => c.InterestRate, f => Math.Round(f.Random.Decimal(4.9m, 24.9m), 2))
            .RuleFor(c => c.MonthlyPayment, (f, c) =>
            {
                var monthlyRate = c.InterestRate / 100 / 12;
                var numerator = c.CreditAmount * monthlyRate;
                var denominator = (decimal)(1 - Math.Pow((double)(1 + monthlyRate), -c.CreditTermMonths));
                
                if (denominator == 0)
                {
                    return Math.Round(c.CreditAmount / c.CreditTermMonths, 2);
                }
                
                return Math.Round(numerator / denominator, 2);
            });
    }

    /// <summary>
    /// Генерирует одну случайную кредитную заявку
    /// </summary>
    public CreditApplication Generate() => _faker.Generate();
}
