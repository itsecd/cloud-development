using System.Text.Json.Nodes;

namespace ContractGenerator.FileService.Storage;

/// <summary>
/// Сервис работы с JSON-файлами сотрудников в объектном хранилище.
/// </summary>
public interface IEmployeeFileStorage
{
    /// <summary>
    /// Создает бакет, если он еще не существует.
    /// </summary>
    public Task EnsureBucketExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохраняет JSON-представление сотрудника в S3.
    /// </summary>
    /// <param name="employeeJson">JSON сотрудника с полем id.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public Task SaveEmployeeJsonAsync(string employeeJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает список ключей файлов в бакете.
    /// </summary>
    public Task<IReadOnlyList<string>> ListKeysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Загружает JSON-файл по ключу.
    /// </summary>
    /// <param name="key">Ключ объекта в S3.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public Task<JsonNode?> DownloadAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Создает ключ файла для сотрудника.
    /// </summary>
    /// <param name="id">Идентификатор сотрудника.</param>
    public static string KeyFor(int id) => $"employee_{id}.json";
}
