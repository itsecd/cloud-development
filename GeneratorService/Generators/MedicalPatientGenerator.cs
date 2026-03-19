using Bogus;
using GeneratorService.Models;

namespace GeneratorService.Generators;

public static class MedicalPatientGenerator
{
    private static readonly Faker<MedicalPatient> _faker = new Faker<MedicalPatient>("ru")
        .RuleFor(p => p.Id, _ => 0)
        .RuleFor(p => p.FullName, f =>
            $"{f.Name.LastName()} {f.Name.FirstName()} {f.Name.LastName()}ович")
        .RuleFor(p => p.Address, f => f.Address.FullAddress())
        .RuleFor(p => p.BirthDate, f => f.Date.PastDateOnly(100))
        .RuleFor(p => p.Height, f => Math.Round(f.Random.Double(50.0, 220.0), 2))
        .RuleFor(p => p.Weight, f => Math.Round(f.Random.Double(2.5, 200.0), 2))
        .RuleFor(p => p.BloodGroup, f => f.Random.Int(1, 4))
        .RuleFor(p => p.RhFactor, f => f.Random.Bool())
        .RuleFor(p => p.LastExaminationDate, (f, p) =>
            f.Date.BetweenDateOnly(p.BirthDate, DateOnly.FromDateTime(DateTime.Today)))
        .RuleFor(p => p.IsVaccinated, f => f.Random.Bool());

    public static MedicalPatient Generate(int id)
    {
        var generated = _faker.Generate();
        return new MedicalPatient
        {
            Id = id,
            FullName = generated.FullName,
            Address = generated.Address,
            BirthDate = generated.BirthDate,
            Height = generated.Height,
            Weight = generated.Weight,
            BloodGroup = generated.BloodGroup,
            RhFactor = generated.RhFactor,
            LastExaminationDate = generated.LastExaminationDate,
            IsVaccinated = generated.IsVaccinated
        };
    }
}