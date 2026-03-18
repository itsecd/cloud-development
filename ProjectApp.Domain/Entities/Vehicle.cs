namespace ProjectApp.Domain.Entities;

/// <summary>
/// Сущность транспортного средства
/// </summary>
public class Vehicle
{
    public required int Id { get; set; }
    public required string Brand { get; set; }
    public required string Model { get; set; }
    public required string RegistrationNumber { get; set; }
    public required string OwnerName { get; set; }
    public int Year { get; set; }
    public decimal EngineVolume { get; set; }
    public int Mileage { get; set; }
    public required string FuelType { get; set; }
    public decimal Price { get; set; }
}
