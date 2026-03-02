using Bogus;
using Inventory.ApiService.Entity;

namespace Inventory.ApiService.Generation;
/// <summary>
/// Сервис генерации тестовых данных продукта.Использует библиотеку Bogus для создания случайных значений.
/// </summary>
public class Generator
{
    private static readonly Faker<Product> _faker = new Faker<Product>()
        .RuleFor(x => x.NameProduct, f => f.Commerce.ProductName())
        .RuleFor(x => x.Category, f => f.Commerce.Categories(1)[0])
        .RuleFor(x => x.Quantity, f => f.Random.Int(0, 1000))
        .RuleFor(x => x.Price, f => Math.Round(f.Random.Decimal(1, 10000), 2))
        .RuleFor(x => x.Weight, f => Math.Round(f.Random.Double(0.1, 100), 2))
        .RuleFor(x => x.Dimension, f =>
        {
            var a = f.Random.Int(1, 200);
            var b = f.Random.Int(1, 200);
            var c = f.Random.Int(1, 200);
            return $"{a}×{b}×{c} cm";
        })
        .RuleFor(x => x.IsFragile, f => f.Random.Bool())
        .RuleFor(x => x.LastDeliveryDate, f => DateOnly.FromDateTime(f.Date.Past(2)))
        .RuleFor(x => x.NextDeliveryDate, (f, item) =>
        {
            var lastDate = item.LastDeliveryDate.ToDateTime(TimeOnly.MinValue);
            var nextDate = f.Date.Between(lastDate, lastDate.AddMonths(6));
            return DateOnly.FromDateTime(nextDate);
        });

    /// <summary>
    /// Генерирует продукт по заданному идентификатору.
    /// </summary>
    /// <param name="id"> Идентификатор продукта</param>
    /// <returns> Сгенерированный объект продукта</returns>
    public Product Generate(int id)
    {
        var product = _faker.Generate();
        product.Id = id;
        return product;
    }
}