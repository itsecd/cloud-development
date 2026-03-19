namespace Domain.Catalog;

/// <summary>
/// Класс Модель + производитель
/// </summary>
public class VehicleCatalog
{
    /// <summary>
    /// Поле отвечающее за Производителя 
    /// </summary>
    public required string Manufacturer { get; set; }
    /// <summary>
    /// Поле отвечающее за модель
    /// </summary>
    public required string Model { get; set; }
}