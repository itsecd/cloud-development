using Domain.Contracts;

namespace Domain.Interfaces;

public interface ISnsPublisherService
{
    Task PublishVehicleContractAsync(VehicleContractDto contract, CancellationToken ct = default);
}
