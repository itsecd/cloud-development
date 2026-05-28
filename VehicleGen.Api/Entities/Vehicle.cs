namespace VehicleGen.Api.Entities;

/// <summary>
/// Транспортное средство
/// </summary>
public class Vehicle
{
    /// <summary>
    /// Уникальный идентификатор транспортного средства
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// VIN-номер
    /// </summary>
    public string VinNumber { get; set; }

    /// <summary>
    /// Производитель
    /// </summary>
    public string Maker { get; set; }

    /// <summary>
    /// Модель
    /// </summary>
    public string CarModel { get; set; }

    /// <summary>
    /// Год выпуска
    /// </summary>
    public int ProductionYear { get; set; }

    /// <summary>
    /// Тип корпуса
    /// </summary>
    public string BodyKind { get; set; }

    /// <summary>
    /// Тип топлива
    /// </summary>
    public string FuelKind { get; set; }

    /// <summary>
    /// Цвет корпуса
    /// </summary>
    public string BodyColor { get; set; }

    /// <summary>
    /// Пробег в километрах
    /// </summary>
    public double DistanceKm { get; set; }

    /// <summary>
    /// Дата последнего технического обслуживания
    /// </summary>
    public DateOnly LastService { get; set; }
}
