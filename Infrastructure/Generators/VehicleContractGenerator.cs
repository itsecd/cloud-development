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
    private readonly Faker<VehicleContractDto> _faker = new Faker<VehicleContractDto>()
            .RuleFor(x => x.Year, f => f.Random.Int(1976, DateTime.UtcNow.Year))
            .RuleFor(x => x.Mileage, f => f.Random.Double(0, 500000))
            .RuleFor(x => x.Vin, f => f.Vehicle.Vin())
            .RuleFor(x => x.Manufacturer, f => f.PickRandom(vehicleModelGenerator.Generate()).Make)
            .RuleFor(x => x.Model, (f, v) => f.PickRandom(vehicleModelGenerator.Generate().First(vm => vm.Make == v.Manufacturer).Models))
            .RuleFor(x => x.BodyType, f => f.Vehicle.Type())
            .RuleFor(x => x.FuelType, f => f.Vehicle.Fuel())
            .RuleFor(x => x.Color, f => f.Commerce.Color())
            .RuleFor(x => x.LastServiceDate, (f, x) =>
                DateOnly.FromDateTime(
                    f.Date.Between(
                        new DateTime(x.Year, 1, 1),
                        DateTime.UtcNow.Date)
                ));
    /// <summary>
    /// Функуция для генерации данных через Bogus
    /// </summary>
    public VehicleContractDto Generate(int id)
    {
        logger.LogInformation("Data generation started. id: {id}", id);
        Randomizer.Seed = new Random(id);

        var dto = _faker.Generate();
        dto.SystemId = id;
        logger.LogInformation("Data generation completed. id: {id}", id);
        return dto;
    }
}