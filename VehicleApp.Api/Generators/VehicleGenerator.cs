using Bogus;
using VehicleApp.Api.Models;

namespace VehicleApp.Api.Generators;

/// <summary>
/// Генератор транспортных средств
/// </summary>
public static class VehicleGenerator
{
    private static readonly Faker<Vehicle> _faker = new Faker<Vehicle>()
        .RuleFor(v => v.Vin, f => f.Vehicle.Vin())
        .RuleFor(v => v.Manufacturer, f => f.Vehicle.Manufacturer())
        .RuleFor(v => v.Model, f => f.Vehicle.Model())
        .RuleFor(v => v.Year, f => f.Date.Past(30).Year)
        .RuleFor(v => v.BodyType, f => f.Vehicle.Type())
        .RuleFor(v => v.FuelType, f => f.Vehicle.Fuel())
        .RuleFor(v => v.BodyColor, f => f.Commerce.Color())
        .RuleFor(v => v.Mileage, f => Math.Round(f.Random.Double(0, 500_000), 1))
        .RuleFor(v => v.LastMaintenanceDate, (f, v) =>
            DateOnly.FromDateTime(f.Date.Between(new DateTime(v.Year, 1, 1), DateTime.UtcNow)));

    /// <summary>
    /// Сгенерировать транспортное средство по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор транспортного средства</param>
    /// <returns>Сгенерированное транспортное средство</returns>
    public static Vehicle Generate(int id)
    {
        var vehicle = _faker.Generate();
        vehicle.Id = id;
        return vehicle;
    }
}
