using Bogus;

public class ContractGenerator
{
    public Contract Generate()
    {
        var faker = new Faker<Contract>()
            .RuleFor(c => c.Id, f => Guid.NewGuid())
            .RuleFor(c => c.ProjectName, f => f.Company.CompanyName())
            .RuleFor(c => c.Customer, f => f.Person.FullName)
            .RuleFor(c => c.Budget, f => f.Random.Decimal(10000, 500000))
            .RuleFor(c => c.CreatedAt, f => DateTime.UtcNow);

        return faker.Generate();
    }
}