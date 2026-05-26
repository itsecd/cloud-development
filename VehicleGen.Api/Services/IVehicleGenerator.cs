using VehicleGen.Api.Entities;

namespace VehicleGen.Api.Services;

public interface IVehicleGenerator
{
    Vehicle CreateVehicle(int id);
}
