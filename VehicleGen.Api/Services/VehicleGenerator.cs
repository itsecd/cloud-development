using Bogus;
using VehicleGen.Api.Entities;

namespace VehicleGen.Api.Services;

public class VehicleGenerator : IVehicleGenerator
{
    public Vehicle CreateVehicle(int id)
    {
        var currentYear = DateTime.Now.Year;

        var generator = new Faker<Vehicle>()
            .RuleFor(v => v.Id, id)
            .RuleFor(v => v.VinNumber, f => f.Vehicle.Vin())
            .RuleFor(v => v.Maker, f => f.Vehicle.Manufacturer())
            .RuleFor(v => v.CarModel, f => f.Vehicle.Model())
            .RuleFor(v => v.ProductionYear, f => f.Random.Int(1960, currentYear))
            .RuleFor(v => v.BodyKind, f => f.Vehicle.Type())
            .RuleFor(v => v.FuelKind, f => f.Vehicle.Fuel())
            .RuleFor(v => v.BodyColor, f => f.Commerce.Color())
            .RuleFor(v => v.DistanceKm, f => Math.Round(f.Random.Double(0, 1000000), 2))
            .RuleFor(v => v.LastService, (f, car) =>
                DateOnly.FromDateTime(f.Date.Between(new DateTime(car.ProductionYear, 1, 1), DateTime.Now)));

        return generator.Generate();
    }
}