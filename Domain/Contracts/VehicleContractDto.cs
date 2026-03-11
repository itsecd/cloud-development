using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Contracts;

public class VehicleContractDto
{
    public int SystemId { get; set; }
    public string Vin { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string BodyType { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public double Mileage { get; set; }
    public DateOnly LastServiceDate { get; set; }
}
