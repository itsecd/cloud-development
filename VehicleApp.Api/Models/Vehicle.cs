namespace VehicleApp.Api.Models;

/// <summary>
/// Транспортное средство
/// </summary>
public sealed class Vehicle
{
    /// <summary>
    /// Идентификатор в системе
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// VIN-номер
    /// </summary>
    public required string Vin { get; init; }

    /// <summary>
    /// Производитель
    /// </summary>
    public required string Manufacturer { get; init; }

    /// <summary>
    /// Модель
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// Год выпуска
    /// </summary>
    public int Year { get; init; }

    /// <summary>
    /// Тип корпуса
    /// </summary>
    public required string BodyType { get; init; }

    /// <summary>
    /// Тип топлива
    /// </summary>
    public required string FuelType { get; init; }

    /// <summary>
    /// Цвет корпуса
    /// </summary>
    public required string BodyColor { get; init; }

    /// <summary>
    /// Пробег
    /// </summary>
    public double Mileage { get; init; }

    /// <summary>
    /// Дата последнего техобслуживания
    /// </summary>
    public DateOnly LastMaintenanceDate { get; init; }
}
