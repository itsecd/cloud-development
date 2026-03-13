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
            .RuleFor(p => p.BloodGroup, f => PickWeighted(f, (1, 35), (2, 25), (3, 20), (4, 20)))
            .RuleFor(p => p.RhFactor, f => PickWeighted(f, (true, 85), (false, 15)))
            .RuleFor(p => p.LastExaminationDate, (f, p) => GenerateExaminationDate(f, p.BirthDate))
            .RuleFor(p => p.IsVaccinated, f => PickWeighted(f, (true, 82), (false, 18)));
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
        var patronymicBase = faker.Name.FirstName(gender);
        var patronymicSuffix = gender == Name.Gender.Male ? "ович" : "овна";

        return $"{lastName} {firstName} {patronymicBase}{patronymicSuffix}";
    }

    private static DateOnly GenerateExaminationDate(Faker faker, DateOnly birthDate)
    {
        var birthDateTime = birthDate.ToDateTime(TimeOnly.MinValue);
        var today = DateTime.Today;

        if (birthDateTime >= today)
        {
            return birthDate;
        }

        return DateOnly.FromDateTime(faker.Date.Between(birthDateTime, today));
    }

    private static T PickWeighted<T>(Faker faker, params (T value, int weight)[] items)
    {
        var totalWeight = items.Sum(item => item.weight);
        var roll = faker.Random.Int(1, totalWeight);
        var currentWeight = 0;

        foreach (var item in items)
        {
            currentWeight += item.weight;
            if (roll <= currentWeight)
            {
                return item.value;
            }
        }

        return items[^1].value;
    }
}
