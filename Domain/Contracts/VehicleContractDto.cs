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
    public required string Vin { get; set; } 
    /// <summary>
    /// Производитель(компания)
    /// </summary>
    public required string Manufacturer { get; set; } 
    /// <summary>
    /// Модель транспортного средства
    /// </summary>
    public required string Model { get; set; }
    /// <summary>
    /// Год выпуска
    /// </summary>
    public required int Year { get; set; }
    /// <summary>
    /// Тип кузова
    /// </summary>
    public required string BodyType { get; set; }
    /// <summary>
    /// Тип топлива
    /// </summary>
    public required string FuelType { get; set; }
    /// <summary>
    /// Цвет транспортного средства
    /// </summary>
    public required string Color { get; set; }
    /// <summary>
    /// Пробег траспортного средства
    /// </summary>
    public required double Mileage { get; set; }
    /// <summary>
    /// Дата последнего обслуживания 
    /// </summary>
    public required DateOnly LastServiceDate { get; set; }
}
