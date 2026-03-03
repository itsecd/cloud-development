using Bogus;
using GenerationService.Entities;
using GenerationService.SeedData;

namespace GenerationService.Services;

/// <summary>
/// Статический генератор тестовых данных для пациентов медицинской базы данных.
/// </summary>
public static class Generator
{
    private static readonly double _maxHeight = 220;
    private static readonly double _minHeight = 130;
    private static readonly double _maxWeight = 250;
    private static readonly double _minWeight = 30;
    private static readonly int _bloodGroupCount = 4;
    private static readonly int _maxAge = 120;

    private static readonly int _digitsRound = 2;

    private static Faker<MedicalPatient> _faker = new Faker<MedicalPatient>("en")
        .RuleFor(m => m.Name, f =>
            $"{f.PickRandom(SeedNames.firstNames)} " +
            $"{f.PickRandom(SeedNames.lastNames)} " +
            $"{f.PickRandom(SeedNames.middleNames)}"
        )
        .RuleFor(m => m.Address, f => f.Address.FullAddress())
        .RuleFor(m => m.BirthDate, f => DateOnly.FromDateTime(f.Date.Past(_maxAge)))
        .RuleFor(m => m.Height, f => Math.Round(f.Random.Double(_minHeight, _maxHeight), _digitsRound))
        .RuleFor(m => m.Weight, f => Math.Round(f.Random.Double(_minWeight, _maxWeight), _digitsRound))
        .RuleFor(m => m.BloodGroup, f => f.Random.Int(1, _bloodGroupCount))
        .RuleFor(m => m.Rh, f => f.Random.Bool())
        .RuleFor(m => m.LastVisit, (f, m) => DateOnly.FromDateTime(f.Date.Between(m.BirthDate!.Value.ToDateTime(TimeOnly.MinValue), DateTime.Now)))
        .RuleFor(m => m.Vaccination, f => f.Random.Bool());

    /// <summary>
    /// Генерирует случайного пациента с указанным идентификатором.
    /// </summary>
    /// <param name="id">Идентификатор пациента.</param>
    /// <returns>Сгенерированный объект <see cref="MedicalPatient"/> с заполненными полями.</returns>
    public static MedicalPatient GenerateAsync(int id)
    {
        var patient = _faker.Generate();
        patient.Id = id;

        return patient;
    }
}
