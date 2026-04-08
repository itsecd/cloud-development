namespace WarehouseApp.Api.Models;

/// <summary>
/// Товар на складе
/// </summary>
public class WarehouseItem
{
    /// <summary>
    /// Идентификатор в системе
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Наименование товара
    /// </summary>
    public required string ProductName { get; init; }

    /// <summary>
    /// Категория товара
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Количество на складе
    /// </summary>
    public int Quantity { get; init; }

    /// <summary>
    /// Цена за единицу товара
    /// </summary>
    public decimal PricePerUnit { get; init; }

    /// <summary>
    /// Вес единицы товара
    /// </summary>
    public double WeightPerUnit { get; init; }

    /// <summary>
    /// Габариты единицы товара
    /// </summary>
    public required string Dimensions { get; init; }

    /// <summary>
    /// Товар хрупкий
    /// </summary>
    public bool IsFragile { get; init; }

    /// <summary>
    /// Дата последней поставки
    /// </summary>
    public DateOnly LastDeliveryDate { get; init; }

    /// <summary>
    /// Дата следующей поставки
    /// </summary>
    public DateOnly NextDeliveryDate { get; init; }
}
