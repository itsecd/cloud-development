using Bogus;
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
    private readonly ILogger<VehicleModelGenerator> _logger;
    private readonly Faker _faker;
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public VehicleModelGenerator(ILogger<VehicleModelGenerator> logger, Faker? faker = null)
    {
        _filePath = Path.Combine(AppContext.BaseDirectory, "Catalog", "vehicleModels.json"); ;
        _logger = logger;
        _faker = faker ?? new Faker();
    }

    public List<VehicleModelJsonItem> Generate()
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


        var data = JsonSerializer.Deserialize<List<VehicleModelJsonItem>>(json, _jsonSerializerOptions);

        if (data is null || data.Count == 0)
            throw new InvalidOperationException(string.Format("Couldn't deserialize datafile: {0}", _filePath));

        data = data
            .Where(x => !string.IsNullOrWhiteSpace(x.Make) && x.Models is not null && x.Models.Count > 0)
            .ToList();

        _items = data;
        _logger.LogInformation("Make and model data from file loaded successfully. FilePath: {FilePath}", _filePath);
        return _items;
    }
}