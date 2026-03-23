namespace CreditApp.FileService.Services;

/// <summary>
/// Сервис для работы с файловым хранилищем S3.
/// Предоставляет операции загрузки и скачивания файлов из объектного хранилища.
/// </summary>
public interface IS3FileStorage
{
    /// <summary>
    /// Загружает содержимое в S3 по указанному ключу.
    /// </summary>
    /// <param name="key">Ключ (путь) файла в бакете.</param>
    /// <param name="content">Текстовое содержимое файла.</param>
    /// <param name="ct">Токен отмены.</param>
    public Task UploadAsync(string key, string content, CancellationToken ct = default);
}
