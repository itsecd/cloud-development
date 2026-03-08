using System.Text.Json.Serialization;

namespace AspireApp.ApiService.Entities;

/// <summary>
/// Товар на складе
/// </summary>
public class Warehouse
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Наименование товара
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Категория товара
    /// </summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    /// <summary>
    /// Количество на складе
    /// </summary>
    [JsonPropertyName("stockQuantity")]
    public int StockQuantity { get; set; }

    /// <summary>
    /// Цена за единицу товара
    /// </summary>
    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    /// <summary>
    /// Вес единицы товара
    /// </summary>
    [JsonPropertyName("weight")]
    public double Weight { get; set; }

    /// <summary>
    /// Габариты единицы товара 
    /// </summary>
    [JsonPropertyName("dimensions")]
    public string? Dimensions { get; set; }

    /// <summary>
    /// Хрупкий ли товар
    /// </summary>
    [JsonPropertyName("isFragile")]
    public bool IsFragile { get; set; }

    /// <summary>
    /// Дата последней поставки
    /// </summary>
    [JsonPropertyName("lastDeliveryDate")]
    public DateOnly LastDeliveryDate { get; set; }

    /// <summary>
    /// Дата следующей планируемой поставки
    /// </summary>
    [JsonPropertyName("nextDeliveryDate")]
    public DateOnly NextDeliveryDate { get; set; }
}