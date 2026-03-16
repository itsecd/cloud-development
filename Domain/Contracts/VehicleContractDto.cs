namespace Domain.Contracts;

/// <summary>
/// Траспортное средство
/// </summary>
public class VehicleContractDto
{
    /// <summary>
    /// ID объекта
    /// </summary>
    public required int SystemId { get; set; }
    /// <summary>
    /// Вин номер транспортного средства 
    /// </summary>
    public required string Vin { get; set; } = string.Empty;
    /// <summary>
    /// Производитель(компания)
    /// </summary>
    public required string Manufacturer { get; set; } = string.Empty;
    /// <summary>
    /// Модель транспортного средства
    /// </summary>
    public required string Model { get; set; } = string.Empty;
    /// <summary>
    /// Год выпуска
    /// </summary>
    public required int Year { get; set; }
    /// <summary>
    /// Тип кузова
    /// </summary>
    public required string BodyType { get; set; } = string.Empty;
    /// <summary>
    /// Тип топлива
    /// </summary>
    public required string FuelType { get; set; } = string.Empty;
    /// <summary>
    /// Цвет транспортного средства
    /// </summary>
    public required string Color { get; set; } = string.Empty;
    /// <summary>
    /// Пробег траспортного средства
    /// </summary>
    public required double Mileage { get; set; }
    /// <summary>
    /// Дата последнего обслуживания 
    /// </summary>
    public required DateOnly LastServiceDate { get; set; }
}
