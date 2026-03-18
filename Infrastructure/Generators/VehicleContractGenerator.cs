using Bogus;
using Domain.Contracts;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;
namespace Infrastructure.Generators;

/// <summary>
/// Класс для генерации данных
/// </summary>
public class VehicleContractGenerator(IVehicleModelGenerator vehicleModelGenerator, ILogger<VehicleContractGenerator> logger) : IVehicleContractGenerator
{
    private readonly IVehicleModelGenerator _vehicleModelGenerator = vehicleModelGenerator;
    private ILogger<VehicleContractGenerator> _logger = logger;
    private readonly Faker _faker = new();
    /// <summary>
    /// Функуция для генерации данных через Bogus
    /// </summary>
    public VehicleContractDto Generate(int id)
    {
        _logger.LogInformation("Data generation started. id: {id}", id);
        Randomizer.Seed = new Random(id);

        var currentYear = DateTime.UtcNow.Year;
        var vehicleCatalog = _vehicleModelGenerator.Generate();
        var makeItem = _faker.PickRandom(vehicleCatalog);
        var model = _faker.PickRandom(makeItem.Models);

        var vehicleFaker = new Faker<VehicleContractDto>()
            .RuleFor(x => x.Year, f => f.Random.Int(1976, currentYear))
            .RuleFor(x => x.Mileage, f => f.Random.Double(0, 500000))
            .RuleFor(x => x.Vin, f => f.Vehicle.Vin())
            .RuleFor(x => x.Manufacturer, _ => makeItem.Make)
            .RuleFor(x => x.Model, _ => model)
            .RuleFor(x => x.BodyType, f => f.Vehicle.Type())
            .RuleFor(x => x.FuelType, f => f.Vehicle.Fuel())
            .RuleFor(x => x.Color, f => f.Commerce.Color())
            .RuleFor(x => x.LastServiceDate, (f, x) =>
                DateOnly.FromDateTime(
                    f.Date.Between(
                        new DateTime(x.Year, 1, 1),
                        DateTime.UtcNow.Date)
                ));

        var dto = vehicleFaker.Generate();
        dto.SystemId = id;
        _logger.LogInformation("Data generation completed. id: {id}", id);
        return dto;
    }
}
