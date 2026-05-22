using System.Text.Json.Nodes;

namespace File.Service.Storage;

/// <summary>
/// Интерфейс службы для манипуляции файлами в объектном хранилище
/// </summary>
public interface IS3Service
{
    /// <summary>
    /// Отправляет файл в хранилище
    /// </summary>
    /// <param name="fileData">Строковая репрезентация сохраняемого файла</param>
    /// <returns>Признак успешной загрузки</returns>
    public Task<bool> UploadFile(string fileData);

    /// <summary>
    /// Получает список всех файлов из хранилища
    /// </summary>
    /// <returns>Список ключей файлов</returns>
    public Task<List<string>> GetFileList();

    /// <summary>
    /// Получает строковую репрезентацию файла из хранилища
    /// </summary>
    /// <param name="filePath">Ключ файла в бакете</param>
    /// <returns>JSON-представление прочитанного файл</returns>
    public Task<JsonNode> DownloadFile(string filePath);

    /// <summary>
    /// Создаёт S3-бакет при необходимости
    /// </summary>
    public Task EnsureBucketExists();
}
