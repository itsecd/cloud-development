using Bogus;
using Domain.Catalog;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;
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
    private ILogger<VehicleModelGenerator> _logger;

    public VehicleModelGenerator(ILogger<VehicleModelGenerator> logger)
    {
        _filePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Domain", "Catalog", "vehicleModels.json"); ;
        _logger = logger;
    }

    public VehicleCatalog Generate(int? seed = null)
    {
        _logger.LogInformation("Началась генерация производитель + модель. Seed: {}", seed);
        if (seed.HasValue)
            Randomizer.Seed = new Random(seed.Value);

        var items = LoadData();

        var faker = new Faker();

        var makeItem = faker.PickRandom(items);
        var model = faker.PickRandom(makeItem.Models);
        _logger.LogInformation("Данные производитель + модель успешно сгенерированны. Seed: {seed}, Mabufacture: {Make}, Model: {model}", seed, makeItem.Make, model); ;
        return new VehicleCatalog
        {
            Manufacturer = makeItem.Make,
            Model = model
        };
    }
    private List<VehicleModelJsonItem> LoadData()
    {
        _logger.LogInformation("Началась загрузка дадтеса производитель + модель . FilePath: {filePath}", _filePath);
        if (_items is not null)
        {
            _logger.LogInformation("Датасет уде загружен. FilePath: {filePath}", _filePath);
            return _items;
        }

        if (!File.Exists(_filePath))
            _logger.LogWarning("Файл датасета не найден. FilePath: {filePath}", _filePath);
        //throw new FileNotFoundException($"Файл не найден: {_filePath}");

        var json = File.ReadAllText(_filePath);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var data = JsonSerializer.Deserialize<List<VehicleModelJsonItem>>(json, options);

        if (data is null || data.Count == 0)
            _logger.LogWarning("Файл пустой или поврежден. FilePath: {filePath}", _filePath);
        //throw new InvalidOperationException("Файл vehicle models.json пустой или поврежден.");

        data = data
            .Where(x => !string.IsNullOrWhiteSpace(x.Make) && x.Models is not null && x.Models.Count > 0)
            .ToList();

        if (data.Count == 0)
            _logger.LogWarning("В файле нет валидных производителей и моделей. FilePath: {filePath}", _filePath);
        //throw new InvalidOperationException("В файле нет валидных производителей и моделей.");

        _items = data;
        _logger.LogInformation("Данные о производителе и модели из файла успешно загружены. FilePath: {filePath}", _filePath);
        return _items;
    }

}
