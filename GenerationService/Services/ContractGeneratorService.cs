using Bogus;
using Bogus.DataSets;
using GenerationService.Models;

namespace GenerationService.Services;

public class ContractGeneratorService
{
    private readonly Faker<SoftwareProjectContract> _faker;

    public ContractGeneratorService()
    {
        _faker = new Faker<SoftwareProjectContract>("ru")
            .RuleFor(c => c.ProjectName,
                f => f.Commerce.ProductName())

            .RuleFor(c => c.ClientCompany,
                f => f.Company.CompanyName())

            .RuleFor(c => c.ProjectManager, f =>
            {
                var gender = f.PickRandom<Name.Gender>();

                var lastName = f.Name.LastName(gender);
                var firstName = f.Name.FirstName(gender);

                var fatherName = f.Name.FirstName(Name.Gender.Male);

                var middleName = gender == Name.Gender.Male
                    ? $"{fatherName}ович"
                    : $"{fatherName}овна";

                return $"{lastName} {firstName} {middleName}";
            })

            .RuleFor(c => c.StartDate,
    f => DateOnly.FromDateTime(f.Date.Past()))

            .RuleFor(c => c.PlannedEndDate,
                (f, c) => c.StartDate.AddMonths(
                    f.Random.Int(1, 12)))

            .RuleFor(c => c.ActualEndDate,
                (f, c) => f.Random.Bool(0.7f)
                    ? c.PlannedEndDate.AddDays(
                        f.Random.Int(-30, 30))
                    : null)

            .RuleFor(c => c.Budget,
                f => decimal.Parse(
                    f.Finance.Amount(100000, 10000000).ToString()))

            .RuleFor(c => c.ActualCost,
                f => decimal.Parse(
                    f.Finance.Amount(100000, 10000000).ToString()))

            .RuleFor(c => c.CompletionPercentage,
                f => f.Random.Int(0, 100));
    }

    public SoftwareProjectContract Generate(int id)
    {
        return _faker.Clone()
            .RuleFor(c => c.Id, _ => id)
            .Generate();
    }
}