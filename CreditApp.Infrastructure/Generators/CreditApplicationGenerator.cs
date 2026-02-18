using Bogus;
using CreditApp.Application.Interfaces;
using CreditApp.Domain.Dictionaries;
using CreditApp.Domain.Entities;

namespace CreditApp.Infrastructure.Generators;

public class CreditApplicationGenerator(double centralBankRate)
    : ICreditApplicationGenerator
{
    public Task<CreditApplication> GenerateAsync(int id, CancellationToken ct)
    {
        var faker = new Faker();

        var status = faker.PickRandom(CreditDictionaries.Statuses);

        var submissionDate = DateOnly.FromDateTime(
            faker.Date.Between(DateTime.Now.AddYears(-2), DateTime.Now));

        DateOnly? decisionDate = null;
        decimal? approvedAmount = null;

        var requested = Math.Round(faker.Random.Decimal(100_000, 10_000_000), 2);

        if (CreditDictionaries.IsTerminal(status))
        {
            decisionDate = submissionDate.AddDays(faker.Random.Int(1, 30));

            if (status == "Одобрена")
            {
                approvedAmount = Math.Round(
                    faker.Random.Decimal(10_000, requested), 2);
            }
        }

        var interestRate = Math.Round(
            faker.Random.Double(centralBankRate, centralBankRate + 10), 2);

        return Task.FromResult(new CreditApplication
        {
            Id = id,
            CreditType = faker.PickRandom(CreditDictionaries.CreditTypes),
            RequestedAmount = requested,
            TermMonths = faker.Random.Int(6, 360),
            InterestRate = interestRate,
            SubmissionDate = submissionDate,
            HasInsurance = faker.Random.Bool(),
            Status = status,
            DecisionDate = decisionDate,
            ApprovedAmount = approvedAmount
        });
    }
}
