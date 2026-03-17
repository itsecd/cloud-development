using Bogus;
using Bogus.DataSets;
using ProjectGenerator.Domain.Models;

namespace ProjectGenerator.Api.Services;

/// <summary>
/// Генератор программных проектов на основе Bogus
/// </summary>
public class SoftwareProjectGenerator : ISoftwareProjectGenerator
{
    private readonly Faker<SoftwareProject> _faker = new Faker<SoftwareProject>("ru")
        .RuleFor(p => p.Id, _ => 0)
        .RuleFor(p => p.ProjectName, f =>
            $"{f.Commerce.ProductName()} {f.Hacker.Adjective()} {f.Finance.AccountName()} {f.Lorem.Word()}")
        .RuleFor(p => p.Customer, f => f.Company.CompanyName())
        .RuleFor(p => p.ProjectManager, f =>
        {
            var gender = f.PickRandom<Name.Gender>();
            return
                $"{f.Name.LastName(gender)} {f.Name.FirstName(gender)} " +
                $"{GeneratePatronymic(f.Name.FirstName(Name.Gender.Male), gender == Name.Gender.Female)}";
        })
        .RuleFor(p => p.StartDate, f =>
            DateOnly.FromDateTime(f.Date.Past(2)))
        .RuleFor(p => p.PlannedEndDate, (f, p) =>
            p.StartDate.AddDays(f.Random.Int(30, 365)))
        .RuleFor(p => p.ActualEndDate, (f, p) =>
            f.Random.Bool(0.3f)
                ? p.StartDate.AddDays(f.Random.Int(10, 400))
                : null)
        .RuleFor(p => p.Budget, f =>
            Math.Round(f.Finance.Amount(100_000, 100_000_000), 2))
        .RuleFor(p => p.ActualCosts, (f, p) =>
            Math.Round(p.Budget * f.Random.Decimal(0.01m, 1.2m), 2))
        .RuleFor(p => p.CompletionPercentage, (f, p) =>
            p.ActualEndDate.HasValue ? 100 : f.Random.Int(0, 99));

    /// <summary>
    /// Генерация отчества на основе мужского имени
    /// </summary>
    /// <param name="maleName">Мужское имя</param>
    /// <param name="isFemale">Признак женского пола</param>
    /// <returns>Отчество</returns>
    private static string GeneratePatronymic(string maleName, bool isFemale) => maleName switch
    {
        _ when maleName.EndsWith("ий") => maleName[..^2] + (isFemale ? "ьевна" : "ьевич"),
        _ when maleName.EndsWith('ь') => maleName[..^1] + (isFemale ? "евна" : "евич"),
        _ when maleName.EndsWith('й') => maleName[..^1] + (isFemale ? "евна" : "евич"),
        _ when maleName.EndsWith('а') || maleName.EndsWith('я') => maleName[..^1] + (isFemale ? "ична" : "ич"),
        _ => maleName + (isFemale ? "овна" : "ович")
    };

    /// <inheritdoc />
    public SoftwareProject Generate(int id)
    {
        var project = _faker.Generate();
        project.Id = id;
        return project;
    }

}
