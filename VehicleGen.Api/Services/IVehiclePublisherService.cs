using VehicleGen.Api.Entities;

namespace VehicleGen.Api.Services;

public interface IVehiclePublisherService
{
    Task SendVehicleToQueueAsync(Vehicle vehicle);
}