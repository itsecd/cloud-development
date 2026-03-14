using System;
using Bogus;
using Microsoft.Extensions.Logging;
using Patient.Generator.DTO;

namespace Patient.Generator.Generator;

/// <summary>
/// Генератор тестовых данных для медицинских пациентов.
/// </summary>
public sealed class PatientGenerator(ILogger<PatientGenerator> logger)
{
    /// <summary>
    /// Максимальный возраст пациента в годах.
    /// </summary>
    private const int MaxAgeYears = 100;
    /// <summary>
    /// Минимальный рост пациента в сантиметрах.
    /// </summary>
    private const double MinHeight = 50.0;
    /// <summary>
    /// Максимальный рост пациента в сантиметрах.
    /// </summary>
    private const double MaxHeight = 220.0;
    /// <summary>
    /// Минимальный вес пациента в килограммах.
    /// </summary>
    private const double MinWeight = 3.0;
    /// <summary>
    /// Максимальный вес пациента в килограммах.
    /// </summary>
    private const double MaxWeight = 250.0;

    /// <summary>
    /// Faker для генерации тестовых данных пациентов.
    /// </summary>
    private static readonly Faker<PatientDto>_faker = new Faker<PatientDto>("ru")
        .RuleFor(x => x.FullName, f =>
        {
            var gender = f.PickRandom<Bogus.DataSets.Name.Gender>();
            var firstName = f.Name.FirstName(gender);
            var patronymicBase = f.Name.FirstName(gender);
            var patronymic = BuildPatronymic(patronymicBase, gender);
            return $"{f.Name.LastName(gender)} {firstName} {patronymic}";
        })
        .RuleFor(x => x.Address, f => f.Address.FullAddress())
        .RuleFor(x => x.BirthDate, f =>
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var earliestBirthDate = today.AddYears(-MaxAgeYears);
            var totalDays = today.DayNumber - earliestBirthDate.DayNumber;
            var offset = f.Random.Int(0, totalDays);
            var birthDate = earliestBirthDate.AddDays(offset);

            return birthDate > today ? today : birthDate;
        })
        .RuleFor(x => x.Height,
            f => Math.Round(f.Random.Double(MinHeight, MaxHeight), 2, MidpointRounding.AwayFromZero))
        .RuleFor(x => x.Weight,
            f => Math.Round(f.Random.Double(MinWeight, MaxWeight), 2, MidpointRounding.AwayFromZero))
        .RuleFor(x => x.BloodGroup, f => f.Random.Int(1, 4))
        .RuleFor(x => x.RhFactor, f => f.Random.Bool())
        .RuleFor(x => x.LastExaminationDate, (f, dto) =>
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var totalDays = today.DayNumber - dto.BirthDate.DayNumber;
            var offset = f.Random.Int(0, totalDays);
            var examinationDate = dto.BirthDate.AddDays(offset);
            return examinationDate < dto.BirthDate ? dto.BirthDate : examinationDate;
        })
        .RuleFor(x => x.IsVaccinated, f => f.Random.Bool(0.8f));

    /// <summary>
    /// Генерирует случайные данные пациента с указанным идентификатором.
    /// </summary>
    /// <param name="id">Уникальный идентификатор пациента.</param>
    /// <returns>Объект PatientDto со случайно сгенерированными данными пациента.</returns>
    public PatientDto Generate(int id)
    {
        logger.LogInformation("Generating patient for id={id}", id);

        var item = _faker.Generate();
        item.Id = id;

        logger.LogInformation("Patient generated: {@Patient}", new
        {
            item.Id,
            item.FullName,
            item.Address,
            item.BirthDate,
            item.Height,
            item.Weight,
            item.BloodGroup,
            item.RhFactor,
            item.LastExaminationDate,
            item.IsVaccinated
        });

        return item;
    }

    private static string BuildPatronymic(string baseName, Bogus.DataSets.Name.Gender gender)
    {
        var stem = baseName.TrimEnd('а', 'я', 'й', 'ь');

        if (string.IsNullOrWhiteSpace(stem))
        {
            stem = baseName;
        }

        return gender == Bogus.DataSets.Name.Gender.Female
            ? $"{stem}овна"
            : $"{stem}ович";
    }
}
