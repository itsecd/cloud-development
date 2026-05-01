using Bogus;
using GenerationService.Models;

namespace GenerationService.Services;

public class ContractGeneratorService
{
    private readonly Faker _faker = new("ru");

    // Id теперь принимается снаружи (от пользователя)
    public SoftwareProjectContract Generate(int id)
    {
        var startDate = DateOnly.FromDateTime(_faker.Date.Past(2));
        var plannedEnd = startDate.AddDays(_faker.Random.Int(30, 365));

        // Процент выполнения — случайное число от 0 до 100
        var completion = _faker.Random.Int(0, 100);

        // Фактическая дата завершения — только если проект завершён (100%)
        DateOnly? actualEnd = completion == 100
            ? startDate.AddDays(_faker.Random.Int(30, 400))
            : null;

        var budget = Math.Round(_faker.Finance.Amount(500_000, 10_000_000), 2);
        var ratio = _faker.Random.Decimal(0.5m, 1.3m);
        var actualCost = Math.Round(budget * ratio, 2);

        // ФИО: фамилия мужская + имя мужское + отчество мужское
        var lastName = _faker.Name.LastName(Bogus.DataSets.Name.Gender.Male);
        var firstName = _faker.Name.FirstName(Bogus.DataSets.Name.Gender.Male);
        var middleName = _faker.Name.FirstName(Bogus.DataSets.Name.Gender.Male) + "ович";

        return new SoftwareProjectContract(
            Id: id,
            ProjectName: _faker.Commerce.ProductName() + " " +
                                  _faker.Hacker.Noun() + " " +
                                  _faker.Finance.Currency().Description,
            ClientCompany: _faker.Company.CompanyName(),
            ProjectManager: $"{lastName} {firstName} {middleName}",
            StartDate: startDate,
            PlannedEndDate: plannedEnd,
            ActualEndDate: actualEnd,
            Budget: budget,
            ActualCost: actualCost,
            CompletionPercentage: completion
        );
    }
}