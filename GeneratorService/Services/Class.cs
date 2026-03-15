namespace PatientApp.Services;
using Bogus;
using PatientApp.Models;

public class PatientGenerator
{
    public List<Patient> Generate(int count)
    {
        var faker = new Faker<Patient>()
            .RuleFor(p => p.Id, f => Guid.NewGuid())
            .RuleFor(p => p.Name, f => f.Name.FirstName())
            .RuleFor(p => p.Surname, f => Guid.NewGuid())
            .RuleFor(p => p.Patronymic, f => Guid.NewGuid())
            .RuleFor(p => p.Birthday, f => Guid.NewGuid())
            .RuleFor(p => p.Gender, f => Guid.NewGuid())
            .RuleFor(p => p.Diagnosis, f => Guid.NewGuid())
            .RuleFor(p => p.Address, f => Guid.NewGuid())
    }
}
