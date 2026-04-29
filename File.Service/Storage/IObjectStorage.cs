using System.Text.Json.Nodes;

namespace File.Service.Storage;

/// <summary>
/// Контракт службы для работы с объектным хранилищем (S3-совместимым)
/// </summary>
public interface IObjectStorage
{
    /// <summary>
    /// Загружает JSON-документ в объектное хранилище. Имя файла формируется на основе поля <c>id</c> JSON-документа
    /// </summary>
    /// <param name="payload">JSON-строка с данными программного проекта</param>
    /// <returns><c>true</c>, если документ успешно загружен; иначе <c>false</c></returns>
    public Task<bool> UploadProject(string payload);

    /// <summary>
    /// Получает список ключей всех файлов, сохранённых в бакете
    /// </summary>
    /// <returns>Коллекция строковых ключей объектов</returns>
    public Task<List<string>> ListProjects();

    /// <summary>
    /// Скачивает указанный файл из бакета и десериализует его как JSON
    /// </summary>
    /// <param name="key">Ключ файла внутри бакета</param>
    /// <returns>Десериализованный <see cref="JsonNode"/> с содержимым файла</returns>
    public Task<JsonNode> DownloadProject(string key);

    /// <summary>
    /// Создаёт целевой бакет в хранилище, если его ещё нет
    /// </summary>
    /// <returns>Задача, завершающаяся после проверки/создания бакета</returns>
    public Task EnsureBucketExists();
}
