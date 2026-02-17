namespace Domain;

public class Vehicle
{
    public int Id { get; set; }
    public string VinNumber { get; set; }
    public string Maker { get; set; }
    public string Model { get; set; }
    public int Year { get; set; }
    public string TypeBodyCar { get; set; }
    public string TypeFuel { get; set; }
    public string Сolor { get; set; }
    public double Mileage { get; set; }
    public DateOnly LastMaintenance {get; set; }
}