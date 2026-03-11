using Bogus;
using Domain.Catalog;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Generators;

public class VehicleModelJsonItem
{
    public string Make { get; set; } = string.Empty;
    public List<string> Models { get; set; } = new();
}

public class VehicleModelGenerator : IVehicleModelGenerator
{
    private readonly string _filePath;
    private List<VehicleModelJsonItem>? _items;

    public VehicleModelGenerator(string filePath)
    {
        _filePath = filePath;
    }

    public VehicleCatalog Generate(int? seed = null)
    {
        if (seed.HasValue)
            Randomizer.Seed = new Random(seed.Value);

        var items = LoadData();

        var faker = new Faker();

        var makeItem = faker.PickRandom(items);
        var model = faker.PickRandom(makeItem.Models);

        return new VehicleCatalog
        {
            Manufacturer = makeItem.Make,
            Model = model
        };
    }
    private List<VehicleModelJsonItem> LoadData()
    {
        if (_items is not null)
            return _items;

        if (!File.Exists(_filePath))
            throw new FileNotFoundException($"Файл не найден: {_filePath}");

        var json = File.ReadAllText(_filePath);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var data = JsonSerializer.Deserialize<List<VehicleModelJsonItem>>(json, options);

        if (data is null || data.Count == 0)
            throw new InvalidOperationException("Файл vehicle models.json пустой или поврежден.");

        data = data
            .Where(x => !string.IsNullOrWhiteSpace(x.Make) && x.Models is not null && x.Models.Count > 0)
            .ToList();

        if (data.Count == 0)
            throw new InvalidOperationException("В файле нет валидных производителей и моделей.");

        _items = data;
        return _items;
    }

}
