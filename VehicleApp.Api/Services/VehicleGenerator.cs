using Bogus;
using VehicleApp.Api.Models;

namespace VehicleApp.Api.Services;
/// <summary>
/// Генератор транспортных средств со случайными свойствами
/// </summary>
public static class VehicleGenerator
{
    private static readonly Faker<Vehicle> _faker = new Faker<Vehicle>()
        .RuleFor(v => v.Vin, f => f.Vehicle.Vin())
        .RuleFor(v => v.Manufacturer, f => f.Vehicle.Manufacturer())
        .RuleFor(v => v.Model, f => f.Vehicle.Model())
        .RuleFor(v => v.Year, f => f.Random.Int(1980, DateTime.Now.Year))
        .RuleFor(v => v.BodyType, f => f.Vehicle.Type())
        .RuleFor(v => v.FuelType, f => f.Vehicle.Fuel())
        .RuleFor(v => v.BodyColor, f => f.Commerce.Color())
        .RuleFor(v => v.Mileage, f => Math.Round(f.Random.Double(0, 300000), 2))
        .RuleFor(v => v.LastServiceDate, (f, v) => DateOnly.FromDateTime(f.Date.Between(new DateTime(v.Year, 1, 1), DateTime.Now)));

    /// <summary>
    /// Метод генерации ТС
    /// </summary>
    /// <param name="id">Идентификатор ТС</param>
    /// <returns>Транспортное средство</returns>
    public static Vehicle GenerateVehicle(int id)
    {
        var vehicle = _faker.Generate();
        vehicle.Id = id;
        return vehicle;
    }
}
