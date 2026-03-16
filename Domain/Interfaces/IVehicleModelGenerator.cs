using Domain.Catalog;

namespace Domain.Interfaces;

public interface IVehicleModelGenerator
{
    public VehicleCatalog Generate(int? seed = null);
}
