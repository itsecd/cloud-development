using Bogus;
using AspireApp.ApiService.Entities;

namespace AspireApp.ApiService.Generator;

/// <summary>
/// Генератор случайных товаров с использованием Bogus
/// </summary>
public class WarehouseGenerator
{
    private readonly Faker<Warehouse> _faker;

    public WarehouseGenerator()
    {
        _faker = new Faker<Warehouse>()
            .RuleFor(w => w.Id, f => f.IndexFaker + 1) // будет перезаписан позже
            .RuleFor(w => w.Name, f => f.Commerce.ProductName())
            .RuleFor(w => w.Category, f => f.Commerce.Categories(1)[0])
            .RuleFor(w => w.StockQuantity, f => f.Random.Int(0, 1000))
            .RuleFor(w => w.Price, f => decimal.Parse(f.Commerce.Price()))
            .RuleFor(w => w.Weight, f => f.Random.Double(0.1, 50.0))
            .RuleFor(w => w.Dimensions, f => $"{f.Random.Int(10, 100)}x{f.Random.Int(10, 100)}x{f.Random.Int(10, 100)}")
            .RuleFor(w => w.IsFragile, f => f.Random.Bool(0.3f))
            .RuleFor(w => w.LastDeliveryDate, f => DateOnly.FromDateTime(f.Date.Past(30)))
            .RuleFor(w => w.NextDeliveryDate, f => DateOnly.FromDateTime(f.Date.Future(30)));
    }

    /// <summary>
    /// Генерирует один случайный товар (Id будет перезаписан вызывающим кодом)
    /// </summary>
    public Warehouse Generate() => _faker.Generate();
}