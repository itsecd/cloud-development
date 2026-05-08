using System.Text.Json.Nodes;

namespace Cloud.EventSink.S3;

/// <summary>
/// Интерфейс службы для работы с файлами в объектном хранилище
/// </summary>
public interface IS3Service
{
    /// <summary>
    /// Метода отправки файла в S3 хранилище 
    /// </summary>
    /// <param name="fileData">Строковая репрезентация сохраняемого файла</param>
    public Task<bool> UploadFile(string fileData);
    /// <summary>
    /// Метода получения списка файлов из объектного хранилища
    /// </summary>
    /// <returns>Список путей к файлам</returns>
    public Task<List<string>> GetFileList();
    /// <summary>
    /// Метода получения файла из объектного хранилища
    /// </summary>
    /// <param name="filePath">Путь к файлу в хранилище</param>
    /// <returns>Строковое представление файла</returns>
    public Task<JsonNode> DownloadFile(string filePath);
    /// <summary>
    /// Метод проверки существования S3 хранилища и создания его, при необходимости
    /// </summary>
    public Task EnsureBucketExists();
}
