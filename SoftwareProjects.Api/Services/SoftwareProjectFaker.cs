using Bogus;
using Bogus.DataSets;
using SoftwareProjects.Api.Entities;

namespace SoftwareProjects.Api.Services;

/// <summary>
/// Статический генератор данных для программного проекта
/// </summary>
public static class SoftwareProjectFaker
{
    private static readonly Faker<SoftwareProject> _faker = new Faker<SoftwareProject>("ru")
        .RuleFor(p => p.Id, _ => 0)
        .RuleFor(p => p.ProjectName, f =>
            $"{f.Commerce.ProductName()} {f.Hacker.Abbreviation()}")
        .RuleFor(p => p.CustomerCompany, f =>
            f.Commerce.Department())
        .RuleFor(p => p.ProjectManager, f =>
        {
            var gender = f.PickRandom<Name.Gender>();
            var lastName = f.Name.LastName(gender);
            var firstName = f.Name.FirstName(gender);
            var patronymic = GeneratePatronymic(f, gender);
            return $"{lastName} {firstName} {patronymic}";
        })
        .RuleFor(p => p.StartDate, f =>
            DateOnly.FromDateTime(f.Date.Past(2)))
        .RuleFor(p => p.PlannedEndDate, (f, p) =>
            p.StartDate.AddDays(f.Random.Int(30, 365)))
        .RuleFor(p => p.ActualEndDate, (f, p) =>
            f.Random.Bool(0.4f)
                ? p.StartDate.AddDays(f.Random.Int(30, 400))
                : null)
        .RuleFor(p => p.Budget, f =>
            Math.Round(f.Finance.Amount(100_000, 10_000_000), 2))
        .RuleFor(p => p.ActualCosts, (f, p) =>
            Math.Round(p.Budget * f.Random.Decimal(0.3m, 1.2m), 2))
        .RuleFor(p => p.CompletionPercentage, (f, p) =>
            p.ActualEndDate.HasValue ? 100 : f.Random.Int(0, 99));

    /// <summary>
    /// Генерирует программный проект с указанным идентификатором
    /// </summary>
    public static SoftwareProject Generate(int id)
    {
        var project = _faker.Generate();
        project.Id = id;
        return project;
    }

    /// <summary>
    /// Генерирует отчество на основе мужского имени с учётом пола менеджера
    /// </summary>
    private static string GeneratePatronymic(Faker faker, Name.Gender gender)
    {
        var maleName = faker.Name.FirstName(Name.Gender.Male);
        var suffix = gender == Name.Gender.Male ? "ович" : "овна";
        var softSuffix = gender == Name.Gender.Male ? "евич" : "евна";

        if (maleName.EndsWith('а') || maleName.EndsWith('я'))
            return maleName[..^1] + suffix;

        if (maleName.EndsWith('ь'))
            return maleName[..^1] + softSuffix;

        return maleName + suffix;
    }
}
