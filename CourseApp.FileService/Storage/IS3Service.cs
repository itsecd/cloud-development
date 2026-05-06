using System.Text.Json.Nodes;

namespace CourseApp.FileService.Storage;

/// <summary>
/// Интерфейс службы для манипуляции файлами курсов в объектном хранилище S3
/// </summary>
public interface IS3Service
{
    /// <summary>
    /// Загружает JSON курса в бакет под ключом course_{id}.json
    /// </summary>
    /// <param name="fileData">Сырое тело сообщения в формате JSON</param>
    /// <returns>true, если PUT прошёл успешно</returns>
    public Task<bool> UploadFile(string fileData);

    /// <summary>
    /// Возвращает список ключей всех объектов в бакете
    /// </summary>
    public Task<List<string>> GetFileList();

    /// <summary>
    /// Скачивает объект из бакета и возвращает его как JsonNode
    /// </summary>
    /// <param name="key">Ключ объекта (например, course_42.json)</param>
    public Task<JsonNode> DownloadFile(string key);

    /// <summary>
    /// Создаёт бакет, если он ещё не существует
    /// </summary>
    public Task EnsureBucketExists();
}
