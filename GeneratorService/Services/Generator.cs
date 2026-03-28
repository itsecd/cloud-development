using Bogus;
using Bogus.DataSets;
using static System.Math;
using PatientApp.Generator.Models;

namespace PatientApp.Generator.Services;

public class PatientGenerator(ILogger<PatientGenerator> logger)
{
    private readonly Faker<Patient> _faker = new Faker<Patient>("ru")
            .RuleFor(x => x.FullName, GeneratePatientFullName)
            .RuleFor(x => x.Birthday, f => f.Date.PastDateOnly(100))
            .RuleFor(x => x.Address, f => f.Address.FullAddress())
            .RuleFor(x => x.Weight, GenerateWeight)
            .RuleFor(x => x.Height, GenerateHeight)
            .RuleFor(x => x.BloodType, f => f.Random.Int(1, 4))
            .RuleFor(x => x.Resus, f => f.Random.Bool())
            .RuleFor(x => x.Vactination, f => f.Random.Bool())
            .RuleFor(x => x.LastVisit, (f, patient) => f.Date.BetweenDateOnly(patient.Birthday, DateOnly.FromDateTime(DateTime.UtcNow))
            );

    public Patient Generate(int id)
    {
        logger.LogInformation("Generating Patient with ID: {id}", id);
        return _faker.UseSeed(id).RuleFor(x => x.Id, _ => id).Generate();
    }

    private static string GeneratePatientFullName(Faker faker)
    {
        var gender = faker.Person.Gender;
        var firstName = faker.Name.FirstName(gender);
        var lastName = faker.Name.LastName(gender);
        var patronymic = faker.Name.FirstName(Name.Gender.Male) + (gender == Name.Gender.Male ? "еевич" : "еевна");

        return string.Join(' ', firstName, lastName, patronymic);
    }

    private static double GenerateWeight(Faker faker, Patient patient)
    {
        var age = DateTime.UtcNow.Year - patient.Birthday.Year;

        return age switch
        {
            < 3 => Round(faker.Random.Double(3, 15), 2),
            < 12 => Round(faker.Random.Double(15, 50), 2),
            < 18 => Round(faker.Random.Double(40, 80), 2),
            _ => Round(faker.Random.Double(50, 120), 2)
        };
    }

    private static double GenerateHeight(Faker faker, Patient patient)
    {
        var age = DateTime.UtcNow.Year - patient.Birthday.Year;

        return age switch
        {
            < 3 => Round(faker.Random.Double(40, 100), 2),
            < 12 => Round(faker.Random.Double(100, 150), 2),
            < 18 => Round(faker.Random.Double(140, 180), 2),
            _ => Round(faker.Random.Double(150, 200), 2)
        };
    }
}