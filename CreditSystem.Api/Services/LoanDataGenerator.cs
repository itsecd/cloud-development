using Bogus;
using CreditSystem.Domain.Entities;

namespace CreditSystem.Api.Services;

/// <summary>
/// Генератор тестовых данных для кредитных заявок
/// </summary>
public class LoanDataGenerator
{
    private readonly Faker<LoanApplication> _faker;

    public LoanDataGenerator()
    {
        // Используем русскую локаль для правдоподобности
        _faker = new Faker<LoanApplication>("ru")
            .RuleFor(l => l.Id, f => f.Random.Guid())
            .RuleFor(l => l.ApplicantName, f => f.Name.FullName())
            .RuleFor(l => l.RequestedAmount, f => f.Finance.Amount(50000, 5000000, 0))
            .RuleFor(l => l.TermMonths, f => f.Random.Int(6, 120))
            .RuleFor(l => l.MonthlyIncome, f => f.Finance.Amount(30000, 300000, 0))
            .RuleFor(l => l.CreditScore, f => f.Random.Int(300, 850))
            .RuleFor(l => l.CreatedAt, f => f.Date.Recent(30))
            .RuleFor(l => l.Status, (f, l) => 
            {
                // Простая логика: если рейтинг совсем плохой — сразу отказ
                return l.CreditScore < 400 ? "Rejected" : "Pending";
            });
    }

    public LoanApplication Generate() => _faker.Generate();
    
    public IEnumerable<LoanApplication> Generate(int count) => _faker.Generate(count);
}
