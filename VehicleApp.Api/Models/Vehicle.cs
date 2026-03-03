namespace VehicleApp.Api.Models;

/// <summary>
/// Модель транспортного средства
/// </summary>
public class Vehicle
{
    /// <summary>
    /// Идентификатор в системе
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// VIN-номер
    /// </summary>
    public string Vin { get; set; } = string.Empty;

    /// <summary>
    /// Производитель
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    /// Модель
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Год выпуска
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Тип корпуса
    /// </summary>
    public string BodyType { get; set; } = string.Empty;

    /// <summary>
    /// Тип топлива
    /// </summary>
    public string FuelType { get; set; } = string.Empty;

    /// <summary>
    /// Цвет корпуса
    /// </summary>
    public string BodyColor { get; set; } = string.Empty;

    /// <summary>
    /// Пробег (км)
    /// </summary>
    public double Mileage { get; set; }

    /// <summary>
    /// Дата последнего техобслуживания
    /// </summary>
    public DateOnly LastServiceDate { get; set; }
}
