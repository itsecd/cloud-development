using Bogus;
using GenerationService.Models;

namespace GenerationService.Services;

public class ContractGeneratorService
{
    private readonly Faker<SoftwareProjectContract> _faker;

    public ContractGeneratorService()
    {
        _faker = new Faker<SoftwareProjectContract>("ru")
            .CustomInstantiator(f => new SoftwareProjectContract(
                Id: f.Random.Guid(),
                ProjectName: f.Hacker.Verb() + " " + f.Hacker.Noun(),
                ClientCompany: f.Company.CompanyName(),
                TechStack: f.PickRandom(
                                    "C# .NET 8",
                                    "Python FastAPI",
                                    "Java Spring Boot",
                                    "Go + gRPC",
                                    "Node.js TypeScript"),
                TeamSize: f.Random.Int(3, 20),
                Budget: f.Finance.Amount(500_000, 10_000_000),
                StartDate: f.Date.Soon(days: 30),
                Deadline: f.Date.Future(yearsToGoForward: 1),
                ProjectManager: f.Name.FullName()
            ));
    }

    public SoftwareProjectContract Generate()
    {
        return _faker.Generate();
    }
}