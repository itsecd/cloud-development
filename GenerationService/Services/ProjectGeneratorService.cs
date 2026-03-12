using Bogus;
using GenerationService.Models;

namespace GenerationService.Services;

public class ProjectGeneratorService
{
    private readonly Faker<SoftwareProject> _faker;

    private static readonly string[] Languages =
    [
        "C#", "Python", "TypeScript", "Go", "Rust",
        "Java", "Kotlin", "Swift", "C++", "Scala"
    ];

    private static readonly string[] Statuses =
    [
        "Active", "Completed", "Archived", "On Hold", "In Development"
    ];

    private static readonly string[] Licenses =
    [
        "MIT", "Apache 2.0", "GPL-3.0", "BSD-3-Clause",
        "LGPL-2.1", "Mozilla Public License 2.0", "ISC"
    ];

    public ProjectGeneratorService()
    {
        _faker = new Faker<SoftwareProject>("ru")
            .CustomInstantiator(f => new SoftwareProject(
                Id: f.Random.Guid(),
                Name: $"{Capitalize(f.Hacker.Adjective())}.{Capitalize(f.Hacker.Noun())}",
                Description: f.Lorem.Sentence(10),
                ProgrammingLanguage: f.PickRandom(Languages),
                RepositoryUrl: $"https://github.com/{f.Internet.UserName()}/{f.Hacker.Noun().ToLower()}",
                License: f.PickRandom(Licenses),
                Status: f.PickRandom(Statuses),
                TeamSize: f.Random.Int(1, 50),
                StartDate: f.Date.Past(5),
                EndDate: f.Random.Bool(0.4f) ? f.Date.Recent(365) : null,
                StarsCount: f.Random.Int(0, 50000),
                OpenIssuesCount: f.Random.Int(0, 500),
                LeadDeveloper: f.Name.FullName()
            ));
    }

    public SoftwareProject Generate() => _faker.Generate();

    public IReadOnlyList<SoftwareProject> Generate(int count) => _faker.Generate(count);

    private static string Capitalize(string input) =>
        string.IsNullOrWhiteSpace(input)
            ? input
            : char.ToUpperInvariant(input[0]) + input[1..].ToLowerInvariant();
}
