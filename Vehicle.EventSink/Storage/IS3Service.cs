using System.Text.Json.Nodes;

namespace Vehicle.EventSink.Storage;

/// <summary>
/// Интерфейс службы для работы с файлами в объектном хранилище.
/// </summary>
public interface IS3Service
{
    /// <summary>
    /// Отправляет JSON-файл в объектное хранилище.
    /// </summary>
    /// <param name="fileData">Строковое представление JSON-файла.</param>
    public Task<bool> UploadFile(string fileData);

    /// <summary>
    /// Получает список всех файлов из хранилища.
    /// </summary>
    public Task<List<string>> GetFileList();

    /// <summary>
    /// Получает JSON-файл из хранилища.
    /// </summary>
    /// <param name="filePath">Ключ файла в бакете.</param>
    public Task<JsonNode> DownloadFile(string filePath);

    /// <summary>
    /// Создает бакет при необходимости.
    /// </summary>
    public Task EnsureBucketExists();
}
