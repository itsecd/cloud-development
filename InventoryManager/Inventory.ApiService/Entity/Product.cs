namespace Inventory.ApiService.Entity;

/// <summary>
/// Класс, представляющий товар на складе
/// </summary>
public class Product
{
    /// <summary>
    /// Идентификатор в системе
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Наименование товара
    /// </summary>
    public string NameProduct { get; set; } = string.Empty;

    /// <summary>
    /// Категория товара
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Количество на складе
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Цена за единицу товара
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Вес единицы товара
    /// </summary>
    public double Weight { get; set; }

    /// <summary>
    /// Габариты единицы товара
    /// </summary>
    public string Dimension { get; set; } = string.Empty;

    /// <summary>
    /// Товар хрупкий
    /// </summary>
    public bool IsFragile { get; set; }

    /// <summary>
    /// Дата последней поставки
    /// </summary>
    public DateOnly LastDeliveryDate { get; set; }

    /// <summary>
    /// Дата следующей поставки
    /// </summary>
    public DateOnly NextDeliveryDate { get; set; }
}
