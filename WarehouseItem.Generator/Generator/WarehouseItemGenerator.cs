using Bogus;
using WarehouseItem.Generator.DTO;

namespace WarehouseItem.Generator.Generator;

/// <summary>
/// Генератор тестовых данных для товаров на складе
/// </summary>
public sealed class WarehouseItemGenerator(ILogger<WarehouseItemGenerator> logger)
{
    /// <summary>
    /// Максимальный размер запаса товара на складе
    /// </summary>
    private const int MaxStockQuantity = 25000;
    /// <summary>
    /// Минимальная цена за единицу товара
    /// </summary>
    private const decimal MinUnitPrice = 5m;
    /// <summary>
    /// Максимальная цена за единицу товара
    /// </summary>
    private const decimal MaxUnitPrice = 250_000m;
    /// <summary>
    /// Минимальный вес единицы товара в килограммах
    /// </summary>
    private const double MinUnitWeight = 0.01;
    /// <summary>
    /// Максимальный вес единицы товара в килограммах
    /// </summary>
    private const double MaxUnitWeight = 250.0;
    /// <summary>
    /// Минимальный размер измерения в сантиметрах
    /// </summary>
    private const int MinDimensionCm = 1;
    /// <summary>
    /// Максимальный размер измерения в сантиметрах
    /// </summary>
    private const int MaxDimensionCm = 99;
    /// <summary>
    /// Максимальное количество дней назад для даты последней поставки
    /// </summary>
    private const int MaxLastDeliveryDaysAgo = 365;

    /// <summary>
    /// Faker для генерации тестовых данных товаров
    /// </summary>
    private static readonly Faker<WarehouseItemDto>_faker = new Faker<WarehouseItemDto>("ru")
        .RuleFor(x => x.ProductName, f => f.Commerce.ProductName())
        .RuleFor(x => x.Category, f => f.Commerce.Department(1))
        .RuleFor(x => x.StockQuantity, f => f.Random.Int(0, MaxStockQuantity))
        .RuleFor(x => x.UnitPrice,
            f => Math.Round(f.Random.Decimal(MinUnitPrice, MaxUnitPrice), 2, MidpointRounding.AwayFromZero))
        .RuleFor(x => x.UnitWeight,
            f => Math.Round(f.Random.Double(MinUnitWeight, MaxUnitWeight), 2, MidpointRounding.AwayFromZero))
        .RuleFor(x => x.UnitDimensions, f =>
        {
            var a = f.Random.Int(MinDimensionCm, MaxDimensionCm);
            var b = f.Random.Int(MinDimensionCm, MaxDimensionCm);
            var c = f.Random.Int(MinDimensionCm, MaxDimensionCm);
            return $"{a:D2}x{b:D2}x{c:D2} см";
        })
        .RuleFor(x => x.IsFragile, f => f.Random.Bool(0.25f))
        .RuleFor(x => x.LastDeliveryDate, f =>
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var daysAgo = f.Random.Int(0, MaxLastDeliveryDaysAgo);
            return today.AddDays(-daysAgo);
        })
        .RuleFor(x => x.NextDeliveryDate, (f, dto) => dto.LastDeliveryDate.AddDays(f.Random.Int(0, 90)));

    /// <summary>
    /// Генерирует случайные данные товара с указанным идентификатором
    /// </summary>
    /// <param name="id">Уникальный идентификатор товара</param>
    /// <returns>Объект WarehouseItemDto с случайно сгенерированными данными</returns>
    public WarehouseItemDto Generate(int id)
    {
        logger.LogInformation("Generating warehouse item for id={id}", id);

        var item = _faker.Generate();
        item.Id = id;

        logger.LogInformation("Warehouse item generated: {@Item}", new
        {
            item.Id,
            item.ProductName,
            item.Category,
            item.StockQuantity,
            item.UnitPrice,
            item.UnitWeight,
            item.UnitDimensions,
            item.IsFragile,
            item.LastDeliveryDate,
            item.NextDeliveryDate
        });

        return item;
    }
}
