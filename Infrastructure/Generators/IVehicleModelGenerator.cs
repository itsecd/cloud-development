namespace Infrastructure.Generators;

public interface IVehicleModelGenerator
{
    public List<VehicleModelJsonItem> Generate();
}
