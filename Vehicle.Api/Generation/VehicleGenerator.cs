using Bogus;
using Vehicle.Api.Entities;

namespace Vehicle.Api.Generation;

/// <summary>
/// Сервис генерации тестовых данных транспортного средства.
/// </summary>
public class VehicleGenerator
{
    private static readonly Faker<VehicleEntity> _faker = new Faker<VehicleEntity>("ru")
        .RuleFor(v => v.Id, f => f.IndexFaker + 1)
        .RuleFor(v => v.Vin, f => f.Vehicle.Vin())
        .RuleFor(v => v.Manufacturer, f => f.Vehicle.Manufacturer())
        .RuleFor(v => v.Model, f => f.Vehicle.Model())
        .RuleFor(v => v.Year, f => f.Date.Past(20).Year)
        .RuleFor(v => v.BodyType, f => f.Vehicle.Type())
        .RuleFor(v => v.FuelType, f => f.Vehicle.Fuel())
        .RuleFor(v => v.Color, f => f.Commerce.Color())
        .RuleFor(v => v.Mileage, f => Math.Round(f.Random.Double(0, 400000), 2))
        .RuleFor(v => v.LastServiceDate, (f, item) =>
        {
            var productionDate = new DateTime(item.Year, 1, 1);
            var serviceDate = f.Date.Between(productionDate, DateTime.Today);
            return DateOnly.FromDateTime(serviceDate);
        });

    /// <summary>
    /// Генерирует транспортное средство по заданному id
    /// </summary>
    /// <param name="id">Идентификатор транспортного средства</param>
    /// <returns>Сгенерированный объект транспортного средства</returns>
    public VehicleEntity Generate(int id)
    {
        var vehicle = _faker.Generate();
        vehicle.Id = id;
        return vehicle;
    }
}
