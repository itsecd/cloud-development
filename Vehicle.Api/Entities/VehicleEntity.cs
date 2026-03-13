using System.Text.Json.Serialization;

namespace Vehicle.Api.Entities;

/// <summary>
/// Генератор записи о транспортных средствах
/// </summary>
public class VehicleEntity
{
    /// <summary>
    /// Идентификатор в системе
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Идентификационный номер транспортного средства (VIN-номер)
    /// </summary>
    [JsonPropertyName("vin")]
    public string Vin { get; set; } = string.Empty;

    /// <summary>
    /// Производитель
    /// </summary>
    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    /// Модель
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Год выпуска
    /// </summary>
    [JsonPropertyName("year")]
    public int Year { get; set; }

    /// <summary>
    /// Тип корпуса
    /// </summary>
    [JsonPropertyName("bodyType")]
    public string BodyType { get; set; } = string.Empty;

    /// <summary>
    /// Тип топлива
    /// </summary>
    [JsonPropertyName("fuelType")]
    public string FuelType { get; set; } = string.Empty;

    /// <summary>
    /// Цвет корпуса
    /// </summary>
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    /// <summary>
    /// Пробег
    /// </summary>
    [JsonPropertyName("mileage")]
    public double Mileage { get; set; }

    /// <summary>
    /// Дата последнего техобслуживания
    /// </summary>
    [JsonPropertyName("lastServiceDate")]
    public DateOnly LastServiceDate { get; set; }
}