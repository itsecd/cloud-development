using Bogus;
using VehicleVault.Api.Entities;

namespace VehicleVault.Api.Services;

/// <summary>
/// Реализация сервиса генерации данных транспортного средства
/// </summary>
public class VehicleGeneratorService : IVehicleGeneratorService
{
    private readonly Faker<Vehicle> _faker = new Faker<Vehicle>()
        .RuleFor(v => v.Vin, f => f.Vehicle.Vin())
        .RuleFor(v => v.Manufacturer, f => f.Vehicle.Manufacturer())
        .RuleFor(v => v.Model, f => f.Vehicle.Model())
        .RuleFor(v => v.Year, f => f.Random.Int(1960, DateTime.Now.Year))
        .RuleFor(v => v.BodyType, f => f.Vehicle.Type())
        .RuleFor(v => v.FuelType, f => f.Vehicle.Fuel())
        .RuleFor(v => v.BodyColor, f => f.Commerce.Color())
        .RuleFor(v => v.Mileage, f => Math.Round(f.Random.Double(0, 1000000), 3))
        .RuleFor(v => v.LastServiceDate, (f, v) =>
            DateOnly.FromDateTime(f.Date.Between(new DateTime(v.Year, 1, 1), DateTime.Now)));

    /// <inheritdoc />
    public Vehicle Generate(int id)
    {
        var vehicle = _faker.Generate();
        vehicle.SystemId = id;
        return vehicle;
    }
}
