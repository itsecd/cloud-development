namespace WarehouseItem.Generator.DTO;

public sealed class WarehouseItemDto
{
    /// <summary>
    /// Уникальный идентификатор товара
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Название продукта
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Категория товара
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Количество единиц на складе
    /// </summary>
    public int StockQuantity { get; set; }

    /// <summary>
    /// Цена за единицу товара
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Вес одной единицы в килограммах
    /// </summary>
    public double UnitWeight { get; set; }

    /// <summary>
    /// Размеры одной единицы товара
    /// </summary>
    public string UnitDimensions { get; set; } = string.Empty;

    /// <summary>
    /// Флаг хрупкого товара
    /// </summary>
    public bool IsFragile { get; set; }

    /// <summary>
    /// Дата последней поставки
    /// </summary>
    public DateOnly LastDeliveryDate { get; set; }

    /// <summary>
    /// Дата планируемой следующей поставки
    /// </summary>
    public DateOnly NextDeliveryDate { get; set; }
}
