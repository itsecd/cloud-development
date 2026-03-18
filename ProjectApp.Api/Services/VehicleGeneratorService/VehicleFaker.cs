using Bogus;
using ProjectApp.Domain.Entities;

namespace ProjectApp.Api.Services.VehicleGeneratorService;

public class VehicleFaker
{
    private readonly Faker<Vehicle> _faker;

    public VehicleFaker()
    {
        var fuelTypes = new[] { "Бензин", "Дизель", "Электро", "Гибрид", "Газ" };

        _faker = new Faker<Vehicle>("ru")
            .RuleFor(v => v.Id, f => f.IndexFaker + 1)
            .RuleFor(v => v.Brand, f => f.PickRandom(
                "Toyota", "BMW", "Mercedes", "Volkswagen", "Hyundai",
                "Ford", "Kia", "Audi", "Nissan", "Renault"))
            .RuleFor(v => v.Model, (f, v) => f.PickRandom(
                "Comfort", "Sport", "Elite", "Plus", "Pro",
                "Active", "Max", "Base", "Premium", "Line"))
            .RuleFor(v => v.RegistrationNumber, f =>
            {
                var letters = "АВЕКМНОРСТУХ";
                var letter1 = letters[f.Random.Int(0, letters.Length - 1)];
                var digits = f.Random.Int(100, 999);
                var letter2 = letters[f.Random.Int(0, letters.Length - 1)];
                var letter3 = letters[f.Random.Int(0, letters.Length - 1)];
                var region = f.Random.Int(10, 199);
                return $"{letter1}{digits}{letter2}{letter3} {region}";
            })
            .RuleFor(v => v.OwnerName, f => f.Name.FullName())
            .RuleFor(v => v.Year, f => f.Random.Int(1984, 2026))
            .RuleFor(v => v.EngineVolume, f => Math.Round(f.Random.Decimal(0.8m, 6.0m), 1))
            .RuleFor(v => v.Mileage, f => f.Random.Int(0, 500000))
            .RuleFor(v => v.FuelType, f => f.PickRandom(fuelTypes))
            .RuleFor(v => v.Price, f => Math.Round(f.Random.Decimal(100000m, 10000000m), 0));
    }

    public Vehicle Generate() => _faker.Generate();
}
