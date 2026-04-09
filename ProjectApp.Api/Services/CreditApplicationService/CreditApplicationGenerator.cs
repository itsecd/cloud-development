using Bogus;
using Microsoft.Extensions.Options;
using ProjectApp.Api.Options;
using ProjectApp.Domain.Entities;

namespace ProjectApp.Api.Services.CreditApplicationService;

public class CreditApplicationGenerator
{
    private readonly Faker<CreditApplication> _faker;
    private readonly CreditApplicationGenerationOptions _options;

    public CreditApplicationGenerator(IOptions<CreditApplicationGenerationOptions> options)
    {
        _options = options.Value;

        var nonTerminalStatuses = new[] { "Новая", "В обработке" };
        var terminalStatuses = new[] { "Одобрена", "Отклонена" };
        var minApplicationDate = DateTime.Today.AddYears(-_options.MaxApplicationAgeYears);
        var maxApplicationDate = DateTime.Today.AddDays(-1);

        _faker = new Faker<CreditApplication>("ru")
            .RuleFor(c => c.Id, f => f.IndexFaker + 1)
            .RuleFor(c => c.CreditType, f => f.PickRandom(_options.CreditTypes))
            .RuleFor(c => c.RequestedAmount, f => Math.Round(
                f.Finance.Amount(_options.MinRequestedAmount, _options.MaxRequestedAmount), 2))
            .RuleFor(c => c.TermMonths, f => f.Random.Int(_options.MinTermMonths, _options.MaxTermMonths))
            .RuleFor(c => c.InterestRate, f =>
            {
                var generatedRate = f.Random.Double(_options.CentralBankKeyRate, _options.CentralBankKeyRate + 12.0);
                return Math.Round(generatedRate, 2);
            })
            .RuleFor(c => c.ApplicationDate, f =>
            {
                var date = f.Date.Between(minApplicationDate, maxApplicationDate);
                return DateOnly.FromDateTime(date);
            })
            .RuleFor(c => c.RequiresInsurance, f => f.Random.Bool())
            .RuleFor(c => c.Status, f =>
            {
                var isTerminal = f.Random.Bool(0.7f);
                return isTerminal ? f.PickRandom(terminalStatuses) : f.PickRandom(nonTerminalStatuses);
            })
            .RuleFor(c => c.DecisionDate, (f, c) =>
            {
                if (c.Status is not ("Одобрена" or "Отклонена"))
                {
                    return null;
                }

                var minDate = c.ApplicationDate.ToDateTime(TimeOnly.MinValue).AddDays(1);
                var maxDate = DateTime.Today;

                if (minDate > maxDate)
                {
                    return DateOnly.FromDateTime(minDate);
                }

                var endDate = f.Date.Between(minDate, maxDate);
                return DateOnly.FromDateTime(endDate);
            })
            .RuleFor(c => c.ApprovedAmount, (f, c) =>
            {
                if (c.Status != "Одобрена")
                {
                    return null;
                }

                var approved = f.Finance.Amount(_options.MinRequestedAmount, c.RequestedAmount);
                return Math.Round(approved, 2);
            });
    }

    public CreditApplication Generate() => _faker.Generate();
}
