namespace VehicleApi.Models;

public record Vehicle
{
    public int Id { get; init; }
    public string Vin { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public string BodyType { get; init; } = string.Empty;
    public string FuelType { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public double Mileage { get; init; }
    public DateOnly LastServiceDate { get; init; }
}
