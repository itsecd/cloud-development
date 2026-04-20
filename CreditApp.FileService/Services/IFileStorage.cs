using System.Text.Json.Nodes;

namespace CreditApp.FileService.Services;

/// <summary>
/// Интерфейс службы для работы с файловым хранилищем S3
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Сохраняет файл в бакет
    /// </summary>
    public Task SaveAsync(string fileName, byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает список ключей всех файлов в бакете
    /// </summary>
    public Task<List<string>> GetFileListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает содержимое файла по ключу в виде JSON
    /// </summary>
    public Task<JsonNode> DownloadAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверяет существование бакета и создаёт его при необходимости
    /// </summary>
    public Task EnsureBucketExists();
}
