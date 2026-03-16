using Bogus;
using Domain.Catalog;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;


namespace Infrastructure.Generators;

/// <summary>
/// Класс для хранения производителя и всех его моделей
/// </summary>
public class VehicleModelJsonItem
{
    public string Make { get; set; } = string.Empty;
    public List<string> Models { get; set; } = new();
}

/// <summary>
///  Класс для генерации правильных данных производитель + модель 
/// </summary>
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
        _logger.LogInformation("Generation of make + model started. Seed: {Seed}", seed);
        if (seed.HasValue)
            Randomizer.Seed = new Random(seed.Value);


        var items = LoadData();

        var faker = new Faker();

        var makeItem = faker.PickRandom(items);
        var model = faker.PickRandom(makeItem.Models);
        _logger.LogInformation("Make + model data generated successfully. Seed: {Seed}, Manufacture: {Make}, Model: {Model}", seed, makeItem.Make, model); ;
        return new VehicleCatalog
        {
            Manufacturer = makeItem.Make,
            Model = model
        };
    }
    private List<VehicleModelJsonItem> LoadData()
    {
        _logger.LogInformation("Loading of make + model dataset started. FilePath: {FilePath}", _filePath);
        if (_items is not null)
        {
            _logger.LogInformation("Dataset already loaded. FilePath: {FilePath}", _filePath);
            return _items;
        }


        if (!File.Exists(_filePath))
            _logger.LogWarning("Dataset file not found. FilePath: {FilePath}", _filePath);
        //throw new FileNotFoundException($"Файл не найден: {_filePath}");


        var json = File.ReadAllText(_filePath);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var data = JsonSerializer.Deserialize<List<VehicleModelJsonItem>>(json, options);

        if (data is null || data.Count == 0)
            _logger.LogWarning("File is empty or corrupted. FilePath: {FilePath}", _filePath);
        //throw new InvalidOperationException("Файл vehicle models.json пустой или поврежден.");

        data = data
            .Where(x => !string.IsNullOrWhiteSpace(x.Make) && x.Models is not null && x.Models.Count > 0)
            .ToList();

        if (data.Count == 0)
            _logger.LogWarning("No valid makes and models in file. FilePath: {FilePath}", _filePath);
        //throw new InvalidOperationException("В файле нет валидных производителей и моделей.");

        _items = data;
        _logger.LogInformation("Make and model data from file loaded successfully. FilePath: {FilePath}", _filePath);
        return _items;
    }


}
