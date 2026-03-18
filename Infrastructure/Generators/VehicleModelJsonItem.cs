namespace Infrastructure.Generators;

/// <summary>
/// Класс для хранения производителя и всех его моделей
/// </summary>
public class VehicleModelJsonItem
{
    /// <summary>
    /// Поле производитель ТО
    /// </summary>
    public required string Make { get; set; }
    /// <summary>
    /// Поле Модель ТО
    /// </summary>
    public List<string> Models { get; set; } = new();
}
