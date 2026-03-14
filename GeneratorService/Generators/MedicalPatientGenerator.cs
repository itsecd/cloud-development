using Bogus;
using GeneratorService.Models;

namespace GeneratorService.Generators;

public static class MedicalPatientGenerator
{
    public static MedicalPatient Generate(int id)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var faker = new Faker<MedicalPatient>("ru")
            .RuleFor(p => p.Id, _ => id)
            .RuleFor(p => p.FullName, f =>
                $"{f.Name.LastName()} {f.Name.FirstName()} {f.Name.FirstName()}")
            .RuleFor(p => p.Address, f => f.Address.FullAddress())
            .RuleFor(p => p.BirthDate, f =>
            {
                var minDate = today.AddYears(-100);
                var totalDays = (int)(today.ToDateTime(TimeOnly.MinValue) - minDate.ToDateTime(TimeOnly.MinValue)).TotalDays;
                return minDate.AddDays(f.Random.Int(0, totalDays));
            })
            .RuleFor(p => p.Height, f => Math.Round(f.Random.Double(50.0, 220.0), 2))
            .RuleFor(p => p.Weight, f => Math.Round(f.Random.Double(2.5, 200.0), 2))
            .RuleFor(p => p.BloodGroup, f => f.Random.Int(1, 4))
            .RuleFor(p => p.RhFactor, f => f.Random.Bool())
            .RuleFor(p => p.LastExaminationDate, (f, p) =>
            {
                var totalDays = (int)(today.ToDateTime(TimeOnly.MinValue) - p.BirthDate.ToDateTime(TimeOnly.MinValue)).TotalDays;
                return totalDays <= 0 ? today : p.BirthDate.AddDays(f.Random.Int(0, totalDays));
            })
            .RuleFor(p => p.IsVaccinated, f => f.Random.Bool());

        return faker.Generate();
    }
}
