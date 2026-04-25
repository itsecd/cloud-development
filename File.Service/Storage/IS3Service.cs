using System.Text.Json.Nodes;

namespace File.Service.Storage;

/// <summary>
/// Сервис работы с Minio-бакетом, в который сохраняются ТС из SNS-уведомлений
/// </summary>
public interface IS3Service
{
    /// <summary>
    /// Создаёт бакет, если его ещё нет
    /// </summary>
    Task EnsureBucketExists();

    /// <summary>
    /// Сохраняет JSON-тело SNS-сообщения в бакет под ключом <c>vehicle_{id}.json</c>
    /// </summary>
    /// <param name="fileData">Сериализованное ТС</param>
    /// <returns><c>true</c> при успешной загрузке</returns>
    Task<bool> UploadFile(string fileData);

    /// <summary>
    /// Возвращает список ключей всех объектов в бакете
    /// </summary>
    Task<List<string>> GetFileList();

    /// <summary>
    /// Читает объект из бакета и парсит его как JSON
    /// </summary>
    /// <param name="key">Ключ объекта</param>
    Task<JsonNode> DownloadFile(string key);
}
