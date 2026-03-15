using Domain.Contracts;

namespace Domain.Interfaces;

public interface IVehicleContractCachedService
{
    public Task<VehicleContractDto> GetVehicleContractAsync(int seed);
}