using VehicleGen.Api.Entities;

namespace VehicleGen.Api.Services;

public interface ICacheService
{
    Task<Vehicle?> RetrieveVehicleAsync(int id);
    Task StoreVehicleAsync(Vehicle vehicle, int expirationMinutes);
}