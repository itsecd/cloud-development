using Bogus;
using CreditApp.Application.Interfaces;
using CreditApp.Domain.Dictionaries;
using CreditApp.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace CreditApp.Infrastructure.Generators;

public class CreditApplicationGenerator : ICreditApplicationGenerator
{
    private readonly Faker<CreditApplication> _faker;

    public CreditApplicationGenerator(IConfiguration configuration)
    {
        var centralBankRate = configuration.GetValue("CreditGenerator:CentralBankRate", 16.0);

        _faker = new Faker<CreditApplication>()
            .RuleFor(c => c.CreditType, f => f.PickRandom(CreditDictionaries.CreditTypes))
            .RuleFor(c => c.RequestedAmount, f => Math.Round(f.Random.Decimal(100_000, 10_000_000), 2))
            .RuleFor(c => c.TermMonths, f => f.Random.Int(6, 360))
            .RuleFor(c => c.InterestRate, f => Math.Round(f.Random.Double(centralBankRate, centralBankRate + 10), 2))
            .RuleFor(c => c.SubmissionDate, f => DateOnly.FromDateTime(f.Date.Between(DateTime.Now.AddYears(-2), DateTime.Now)))
            .RuleFor(c => c.HasInsurance, f => f.Random.Bool())
            .RuleFor(c => c.Status, f => f.PickRandom(CreditDictionaries.Statuses))
            .RuleFor(c => c.DecisionDate, (f, c) => CreditDictionaries.IsTerminal(c.Status)
                ? c.SubmissionDate.AddDays(f.Random.Int(1, 30))
                : null)
            .RuleFor(c => c.ApprovedAmount, (f, c) => c.Status == "Одобрена"
                ? Math.Round(f.Random.Decimal(10_000, c.RequestedAmount), 2)
                : null);
    }

    public Task<CreditApplication> GenerateAsync(int id)
    {
        var application = _faker.Generate();
        application.Id = id;
        return Task.FromResult(application);
    }
}
