using Domain.Contracts;
using Infrastructure.Generators;
using Microsoft.Extensions.Logging.Abstractions;

namespace Vehicle.Test;

/// <summary>
///   Класс для тестирования валидатора(VehicleContractValidator) и генератора(VehicleContractGenerator) 
/// </summary>
public class VehicleTests
{
    private readonly VehicleData _data;

    public VehicleTests()
    {
        var logger = NullLogger<VehicleContractGenerator>.Instance;
        var loggerModel = NullLogger<VehicleModelGenerator>.Instance;
        var modelGenertor = new VehicleModelGenerator(loggerModel);
        var generator = new VehicleContractGenerator(modelGenertor, logger);
        _data = new VehicleData(generator);
    }

    [Fact]
    public void ValidateValidData()
    {
        foreach (var dto in _data.VehicleContracts)
        {
            var result = VehicleContractValidator.ValidateBool(dto);
            Assert.True(result);
        }
    }
    [Fact]
    public void ValidateInvalidData()
    {
        foreach (var dto in _data.InvalidVehicleContracts)
        {
            var result = VehicleContractValidator.ValidateBool(dto);
            Assert.False(result);
        }
    }

    [Fact]
    public void ValidateInvalidManualData()
    {
        foreach (var dto in _data.ManualInvalidVehicleContracts)
        {
            var result = VehicleContractValidator.ValidateBool(dto);
            Assert.False(result);
        }
    }
}