namespace VehicleVault.Api.Entities;

/// <summary>
/// Транспортное средство
/// </summary>
public class Vehicle
{
    /// <summary>
    /// Идентификатор в системе
    /// </summary>
    public int SystemId { get; set; }

    /// <summary>
    /// VIN-номер
    /// </summary>
    public required string Vin { get; set; }

    /// <summary>
    /// Производитель
    /// </summary>
    public required string Manufacturer { get; set; }

    /// <summary>
    /// Модель
    /// </summary>
    public required string Model { get; set; }

    /// <summary>
    /// Год выпуска
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Тип корпуса
    /// </summary>
    public required string BodyType { get; set; }

    /// <summary>
    /// Тип топлива
    /// </summary>
    public required string FuelType { get; set; }

    /// <summary>
    /// Цвет корпуса
    /// </summary>
    public required string BodyColor { get; set; }

    /// <summary>
    /// Пробег
    /// </summary>
    public double Mileage { get; set; }

    /// <summary>
    /// Дата последнего техобслуживания
    /// </summary>
    public DateOnly LastServiceDate { get; set; }
}
