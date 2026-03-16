using Bogus;
using Domain.Contracts;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;
namespace Infrastructure.Generators;


public class VehicleContractGenerator : IVehicleContractGenerator
{
    private readonly IVehicleModelGenerator _vehicleModelGenerator;
    private ILogger<VehicleContractGenerator> _logger;
    public VehicleContractGenerator(IVehicleModelGenerator vehicleModelGenerator, ILogger<VehicleContractGenerator> logger)
    {
        _vehicleModelGenerator = vehicleModelGenerator;
        _logger = logger;
    }
    public VehicleContractDto Generate(int seed)
    {
        _logger.LogInformation("Data generation started. Seed: {Seed}", seed);
        Randomizer.Seed = new Random(seed);

        var currentYear = DateTime.UtcNow.Year;
        var vehicleCatalog = _vehicleModelGenerator.Generate();

        var vehicleFaker = new Faker<VehicleContractDto>()
            .RuleFor(x => x.Year, f => f.Random.Int(1976, currentYear))
            .RuleFor(x => x.Mileage, f => f.Random.Double(0, 500000))
            .RuleFor(x => x.Vin, f => f.Vehicle.Vin())
            .RuleFor(x => x.Manufacturer, _ => vehicleCatalog.Manufacturer)
            .RuleFor(x => x.Model, _ => vehicleCatalog.Model)
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
        dto.SystemId = seed;
        if (!VehicleContractValidator.ValidateBool (dto))
        {
            //throw new InvalidOperationException("Сгенерирован невалидный VehicleContractDto.");
            _logger.LogWarning("Invalid data generated. Seed: {Seed}", seed);
        }
        _logger.LogInformation("Data generation completed. Seed: {Seed}", seed);
        return dto;
    }
}
