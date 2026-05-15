using Bogus;
using Bogus.DataSets;
using ProjectApp.Domain.Entities;

namespace ProjectApp.Api.Services.ProjectGeneratorService;

/// <summary>
/// Генератор случайных медицинских пациентов с использованием Bogus
/// </summary>
public class MedicalPatientGenerator
{
    private readonly Faker<MedicalPatient> _faker;

    public MedicalPatientGenerator()
    {
        _faker = new Faker<MedicalPatient>("ru")
            .RuleFor(p => p.Id, f => f.IndexFaker + 1)
            .RuleFor(p => p.FullName, GenerateFullName)
            .RuleFor(p => p.Address, f => f.Address.FullAddress())
            .RuleFor(p => p.BirthDate, f => f.Date.PastDateOnly(90))
            .RuleFor(p => p.Height, f => Math.Round(f.Random.Double(145.0, 205.0), 2))
            .RuleFor(p => p.Weight, f => Math.Round(f.Random.Double(45.0, 140.0), 2))
            .RuleFor(p => p.BloodGroup, f => f.Random.WeightedRandom([1, 2, 3, 4], [0.35f, 0.25f, 0.20f, 0.20f]))
            .RuleFor(p => p.RhFactor, f => f.Random.WeightedRandom([true, false], [0.85f, 0.15f]))
            .RuleFor(p => p.LastExaminationDate, (f, p) => GenerateExaminationDate(f, p.BirthDate))
            .RuleFor(p => p.IsVaccinated, f => f.Random.WeightedRandom([true, false], [0.82f, 0.18f]));
    }

    /// <summary>
    /// Генерирует одного случайного медицинского пациента
    /// </summary>
    public MedicalPatient Generate() => _faker.Generate();

    private static string GenerateFullName(Faker faker)
    {
        var gender = faker.PickRandom<Name.Gender>();
        var lastName = faker.Name.LastName(gender);
        var firstName = faker.Name.FirstName(gender);
        var patronymicBase = faker.Name.FirstName(Name.Gender.Male);
        var patronymicSuffix = gender == Name.Gender.Male ? "ович" : "овна";

        return $"{lastName} {firstName} {patronymicBase}{patronymicSuffix}";
    }

    private static DateOnly GenerateExaminationDate(Faker faker, DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return birthDate >= today ? birthDate : faker.Date.BetweenDateOnly(birthDate, today);
    }
}
