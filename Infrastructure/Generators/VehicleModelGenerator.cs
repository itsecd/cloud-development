using Bogus;
using Domain.Catalog;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;


namespace Infrastructure.Generators;


/// <summary>
///  Класс для генерации правильных данных производитель + модель 
/// </summary>
public class VehicleModelGenerator : IVehicleModelGenerator
{
    private readonly string _filePath;
    private List<VehicleModelJsonItem>? _items;
    private ILogger<VehicleModelGenerator> _logger;
    private readonly Faker _faker;

    public VehicleModelGenerator(ILogger<VehicleModelGenerator> logger, Faker? faker = null)
    {
        _filePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Domain", "Catalog", "vehicleModels.json"); ;
        _logger = logger;
        _faker = faker ?? new Faker();
    }


    public VehicleCatalog Generate(int? id = null)
    {
        _logger.LogInformation("Generation of make + model started. id: {id}", id);
        if (id.HasValue)
            Randomizer.Seed = new Random(id.Value);


        var items = LoadData();


        var makeItem = _faker.PickRandom(items);
        var model = _faker.PickRandom(makeItem.Models);
        _logger.LogInformation("Make + model data generated successfully. id: {id}, Manufacture: {Make}, Model: {Model}", id, makeItem.Make, model); ;
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


        var json = File.ReadAllText(_filePath);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var data = JsonSerializer.Deserialize<List<VehicleModelJsonItem>>(json, options);

        if (data is null || data.Count == 0)
            _logger.LogWarning("File is empty or corrupted. FilePath: {FilePath}", _filePath);

        data = data
            .Where(x => !string.IsNullOrWhiteSpace(x.Make) && x.Models is not null && x.Models.Count > 0)
            .ToList();

        if (data.Count == 0)
            _logger.LogWarning("No valid makes and models in file. FilePath: {FilePath}", _filePath);

        _items = data;
        _logger.LogInformation("Make and model data from file loaded successfully. FilePath: {FilePath}", _filePath);
        return _items;
    }


}
