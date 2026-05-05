using CourseApp.Api.Models;

namespace CourseApp.FileService.Storage;

/// <summary>
/// Интерфейс службы для манипуляции файлами курсов в объектном хранилище S3
/// </summary>
public interface IS3Service
{
    /// <summary>
    /// Сериализует курс в JSON и загружает его в бакет под ключом course_{id}.json
    /// </summary>
    /// <param name="course">Курс, полученный из брокера сообщений</param>
    /// <returns>true, если PUT прошёл успешно</returns>
    public Task<bool> UploadFile(Course course);

    /// <summary>
    /// Возвращает список ключей всех объектов в бакете
    /// </summary>
    public Task<List<string>> GetFileList();

    /// <summary>
    /// Скачивает объект из бакета и десериализует его в Course
    /// </summary>
    /// <param name="key">Ключ объекта (например, course_42.json)</param>
    public Task<Course> DownloadFile(string key);

    /// <summary>
    /// Создаёт бакет, если он ещё не существует
    /// </summary>
    public Task EnsureBucketExists();
}
