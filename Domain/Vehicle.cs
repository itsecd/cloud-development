namespace Domain;

/// <summary>
/// Траспортное средство
/// </summary>
public class Vehicle
{
    /// <summary>
    /// ID объекта
    /// </summary>
    public required int Id { get; set; }
    /// <summary>
    /// Вин номер транспортного средства 
    /// </summary>
    public required string VinNumber { get; set; }
    /// <summary>
    /// Производитель(компания)
    /// </summary>
    public required string Maker { get; set; }
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
    public required string TypeBodyCar { get; set; }
    /// <summary>
    /// Тип топлива
    /// </summary>
    public required string TypeFuel { get; set; }
    /// <summary>
    /// Цвет транспортного средства
    /// </summary>
    public required string Сolor { get; set; }
    /// <summary>
    /// Пробег траспортного средства
    /// </summary>
    public required double Mileage { get; set; }
    /// <summary>
    /// Дата последнего обслуживания 
    /// </summary>
    public required DateOnly LastMaintenance {get; set; }
}