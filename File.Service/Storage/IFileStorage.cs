using System.Text.Json.Nodes;

namespace File.Service.Storage;

/// <summary>
/// Файловое хранилище для сериализованных транспортных средств
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Загружает строковое представление файла в S3 хранилище
    /// </summary>
    /// <param name="payload">JSON-строка с данными транспортного средства</param>
    public Task<bool> UploadAsync(string payload);

    /// <summary>
    /// Получить список ключей всех файлов в бакете
    /// </summary>
    public Task<List<string>> ListAsync();

    /// <summary>
    /// Получить содержимое файла из бакета
    /// </summary>
    /// <param name="key">Ключ файла</param>
    public Task<JsonNode> DownloadAsync(string key);

    /// <summary>
    /// Создать бакет, если он отсутствует
    /// </summary>
    public Task EnsureBucketExistsAsync();
}
