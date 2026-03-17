namespace VehicleApi.Models;

/// <summary>
/// Представляет сущность транспортного средства.
/// </summary>
public record Vehicle
{
    /// <summary>Уникальный идентификатор транспортного средства.</summary>
    public int Id { get; init; }

    /// <summary>Идентификационный номер транспортного средства (VIN).</summary>
    public required string Vin { get; init; }

    /// <summary>Производитель транспортного средства.</summary>
    public required string Manufacturer { get; init; }

    /// <summary>Модель транспортного средства.</summary>
    public required string Model { get; init; }

    /// <summary>Год выпуска транспортного средства.</summary>
    public int Year { get; init; }

    /// <summary>Тип кузова транспортного средства.</summary>
    public required string BodyType { get; init; }

    /// <summary>Тип топлива транспортного средства.</summary>
    public required string FuelType { get; init; }

    /// <summary>Цвет транспортного средства.</summary>
    public required string Color { get; init; }

    /// <summary>Пробег транспортного средства.</summary>
    public double Mileage { get; init; }

    /// <summary>Дата последнего технического обслуживания.</summary>
    public DateOnly LastServiceDate { get; init; }
}