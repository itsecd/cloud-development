
using Bogus;
using Bogus.DataSets;
using Domain.Contracts;
using Domain.Interfaces;
namespace Infrastructure.Generators;

public class VehicleContractGenerator : IVehicleContractGenerator
{
    public VehicleContractDto Generate(int? seed = null)
    {
        if(seed.HasValue)
            Randomizer.Seed = new Random(seed.Value);

        var currentYear = DateTime.UtcNow.Year;

        var vehicleFaker = new Faker<VehicleContractDto>()
            .RuleFor(x => x.Year, f => f.Random.Int(1976, currentYear))
            .RuleFor(x => x.Mileage, f => f.Random.Double(0, 500000))
            .RuleFor(x => x.Vin, f => f.Vehicle.Vin())
            .RuleFor(x => x.Manufacturer, f => f.Vehicle.Manufacturer())
            .RuleFor(x => x.Model, f => f.Vehicle.Model())
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
        if (!VehicleContractValidator.ValidateBool (dto))
        {
            throw new InvalidOperationException("Сгенерирован невалидный VehicleContractDto.");
        }
        return dto;
    }
}
