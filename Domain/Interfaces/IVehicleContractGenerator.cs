using Domain.Contracts;

namespace Domain.Interfaces;

public interface IVehicleContractGenerator
{
    public VehicleContractDto Generate(int? seed = null);
}