using Bogus;
using GenerationService.Models;

namespace GenerationService.Services;

public class ContractGeneratorService
{
    private readonly Faker<SoftwareProjectContract> _faker;

    public ContractGeneratorService()
    {
        _faker = new Faker<SoftwareProjectContract>("ru")
            .CustomInstantiator(f =>
            {
                // Дата начала — в пределах последних 2 лет
                var startDate = DateOnly.FromDateTime(f.Date.Past(2));

                // Плановая дата завершения — позже даты начала
                var plannedEnd = startDate.AddDays(f.Random.Int(30, 365));

                // Фактическая дата завершения — позже даты начала
                var actualEnd = startDate.AddDays(f.Random.Int(30, 400));

                // Бюджет округлён до 2 знаков
                var budget = Math.Round(f.Finance.Amount(500_000, 10_000_000), 2);

                // Фактические затраты пропорциональны бюджету (от 50% до 130%)
                var ratio = f.Random.Decimal(0.5m, 1.3m);
                var actualCost = Math.Round(budget * ratio, 2);

                // Если есть фактическая дата завершения — процент 100
                // (фактическая дата всегда есть в нашей модели, поэтому всегда 100)
                var completion = 100;

                return new SoftwareProjectContract(
                    Id: f.Random.Int(1, 100000),
                    ProjectName: f.Commerce.ProductName() + " " +
                                          f.Hacker.Noun() + " " +
                                          f.Finance.Currency().Description,
                    ClientCompany: f.Company.CompanyName(),
                    ProjectManager: f.Name.LastName() + " " +
                                          f.Name.FirstName() + " " +
                                          f.Name.FirstName(),
                    StartDate: startDate,
                    PlannedEndDate: plannedEnd,
                    ActualEndDate: actualEnd,
                    Budget: budget,
                    ActualCost: actualCost,
                    CompletionPercentage: completion
                );
            });
    }

    public SoftwareProjectContract Generate()
    {
        return _faker.Generate();
    }
}