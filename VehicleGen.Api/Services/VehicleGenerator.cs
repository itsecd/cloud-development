using Bogus;
using VehicleGen.Api.Entities;

namespace VehicleGen.Api.Services;

/// <summary>
/// Реализация генератора данных транспортных средств с помощью Bogus
/// </summary>
public class VehicleGenerator : IVehicleGenerator
{
    private static readonly Faker<Vehicle> _faker = new Faker<Vehicle>()
        .RuleFor(v => v.Id, f => 0)
        .RuleFor(v => v.VinNumber, f => f.Vehicle.Vin())
        .RuleFor(v => v.Maker, f => f.Vehicle.Manufacturer())
        .RuleFor(v => v.CarModel, f => f.Vehicle.Model())
        .RuleFor(v => v.ProductionYear, f => f.Random.Int(1960, DateTime.Now.Year))
        .RuleFor(v => v.BodyKind, f => f.Vehicle.Type())
        .RuleFor(v => v.FuelKind, f => f.Vehicle.Fuel())
        .RuleFor(v => v.BodyColor, f => f.Commerce.Color())
        .RuleFor(v => v.DistanceKm, f => Math.Round(f.Random.Double(0, 1000000), 2))
        .RuleFor(v => v.LastService, (f, car) =>
            DateOnly.FromDateTime(f.Date.Between(new DateTime(car.ProductionYear, 1, 1), DateTime.Now)));

    public Vehicle CreateVehicle(int id)
    {
        var vehicle = _faker.Generate();
        vehicle.Id = id;
        return vehicle;
    }
}