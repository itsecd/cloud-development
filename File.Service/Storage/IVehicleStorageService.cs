using System.Text.Json.Nodes;

namespace File.Service.Storage;

/// <summary>
/// Сервис для манипуляции файлами транспортных средств в S3-хранилище
/// </summary>
public interface IVehicleStorageService
{
    /// <summary>
    /// Создаёт бакет, если он отсутствует
    /// </summary>
    public Task EnsureBucketExists();

    /// <summary>
    /// Сохраняет JSON-представление транспортного средства в S3
    /// </summary>
    /// <param name="vehicleJson">JSON-представление транспортного средства (ожидается поле <c>systemId</c>)</param>
    /// <returns>true, если файл успешно загружен</returns>
    public Task<bool> Upload(string vehicleJson);

    /// <summary>
    /// Возвращает список ключей файлов из бакета
    /// </summary>
    public Task<List<string>> ListKeys();

    /// <summary>
    /// Скачивает файл по ключу и возвращает его JSON-узел
    /// </summary>
    /// <param name="key">Ключ файла</param>
    public Task<JsonNode?> Download(string key);

    /// <summary>
    /// Возвращает ключ файла по идентификатору транспортного средства
    /// </summary>
    /// <param name="id">Идентификатор транспортного средства</param>
    public static string KeyFor(int id) => $"vehicle_{id}.json";
}
