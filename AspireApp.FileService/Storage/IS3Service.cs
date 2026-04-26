using System.Text.Json.Nodes;

namespace AspireApp.FileService.Storage;

/// <summary>
/// Контракт службы для работы с объектным хранилищем S3
/// </summary>
public interface IS3Service
{
    /// <summary>
    /// Загружает файл в бакет. Ключ формируется из поля id в JSON
    /// </summary>
    /// <param name="fileData">Строковое представление JSON-документа</param>
    /// <returns>true если файл успешно загружен</returns>
    public Task<bool> UploadFile(string fileData);

    /// <summary>
    /// Возвращает список ключей всех файлов в бакете
    /// </summary>
    /// <returns>Список ключей объектов</returns>
    public Task<List<string>> GetFileList();

    /// <summary>
    /// Скачивает файл по ключу и парсит его как JSON
    /// </summary>
    /// <param name="key">Ключ объекта в бакете</param>
    /// <returns>Распарсенный JsonNode</returns>
    public Task<JsonNode> DownloadFile(string key);

    /// <summary>
    /// Гарантирует существование бакета. Если бакета нет — создаёт его
    /// </summary>
    public Task EnsureBucketExists();
}
