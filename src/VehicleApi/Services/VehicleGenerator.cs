using Bogus;
using VehicleApi.Models;

namespace VehicleApi.Services;

public static class VehicleGenerator
{
    public static Vehicle Generate(int id)
    {
        var faker = new Faker<Vehicle>()
            .UseSeed(id)
            .RuleFor(v => v.Id, id)
            .RuleFor(v => v.Vin, f => f.Vehicle.Vin())
            .RuleFor(v => v.Manufacturer, f => f.Vehicle.Manufacturer())
            .RuleFor(v => v.Model, f => f.Vehicle.Model())
            .RuleFor(v => v.Year, f => f.Random.Int(1990, DateTime.Now.Year))
            .RuleFor(v => v.BodyType, f => f.Vehicle.Type())
            .RuleFor(v => v.FuelType, f => f.Vehicle.Fuel())
            .RuleFor(v => v.Color, f => f.Commerce.Color())
            .RuleFor(v => v.Mileage, f => f.Random.Double(0, 500000))
            .RuleFor(v => v.LastServiceDate, (f, v) =>
                DateOnly.FromDateTime(f.Date.Between(new DateTime(v.Year, 1, 1), DateTime.Now)));

        return faker.Generate();
    }
}
