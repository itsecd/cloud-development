namespace VehicleGen.Api.Entities;

public class Vehicle
{
    public int Id { get; set; }
    public string VinNumber { get; set; }
    public string Maker { get; set; }
    public string CarModel { get; set; }
    public int ProductionYear { get; set; }
    public string BodyKind { get; set; }
    public string FuelKind { get; set; }
    public string BodyColor { get; set; }
    public double DistanceKm { get; set; }
    public DateOnly LastService { get; set; }
}
