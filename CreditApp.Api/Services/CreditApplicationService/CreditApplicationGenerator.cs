using Bogus;
using CreditApp.Domain.Entities;

namespace CreditApp.Api.Services.CreditApplicationService;

/// <summary>
/// Сервис генерации кредитных заявок с использованием Bogus
/// </summary>
public class CreditApplicationGenerator
{
    private static readonly string[] _creditTypes =
    [
        "Потребительский",
        "Ипотека",
        "Автокредит",
        "Бизнес-кредит",
        "Образовательный"
    ];

    private static readonly string[] _statuses =
    [
        "Новая",
        "В обработке",
        "Одобрена",
        "Отклонена"
    ];

    private static readonly string[] _terminalStatuses = ["Одобрена", "Отклонена"];

    public CreditApplication Generate(int id)
    {
        var faker = new Faker<CreditApplication>("ru")
            .RuleFor(c => c.Id, f => id)
            .RuleFor(c => c.Type, f => f.PickRandom(_creditTypes))
            .RuleFor(c => c.Amount, f => Math.Round(f.Finance.Amount(10000, 10000000), 2))
            .RuleFor(c => c.Term, f => f.Random.Int(6, 360))
            .RuleFor(c => c.InterestRate, f => Math.Round(f.Random.Double(16.0, 25.0), 2))
            .RuleFor(c => c.SubmissionDate, f => f.Date.PastDateOnly(2))
            .RuleFor(c => c.RequiresInsurance, f => f.Random.Bool())
            .RuleFor(c => c.Status, f => f.PickRandom(_statuses))
            .RuleFor(c => c.ApprovalDate, (f, c) =>
            {
                if (!_terminalStatuses.Contains(c.Status))
                    return null;

                return f.Date.BetweenDateOnly(c.SubmissionDate, DateOnly.FromDateTime(DateTime.Today));
            })
            .RuleFor(c => c.ApprovedAmount, (f, c) =>
            {
                if (c.Status != "Одобрена")
                    return null;

                return Math.Round(c.Amount * f.Random.Decimal(0.7m, 1.0m), 2);
            });

        return faker.Generate();
    }
}
