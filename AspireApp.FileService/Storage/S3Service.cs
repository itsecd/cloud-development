using Amazon.S3;
using Amazon.S3.Model;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AspireApp.FileService.Storage;

/// <summary>
/// Служба для работы с объектным хранилищем S3 через AWS SDK
/// </summary>
/// <param name="client">AWS SDK клиент S3</param>
/// <param name="configuration">Конфигурация приложения, содержит имя бакета</param>
/// <param name="logger">Логгер</param>
public class S3Service(IAmazonS3 client, IConfiguration configuration, ILogger<S3Service> logger) : IS3Service
{
    private readonly string _bucketName = configuration["AWS:Resources:S3BucketName"]
        ?? throw new KeyNotFoundException("Имя S3 бакета не найдено в конфигурации");

    /// <summary>
    /// Возвращает список всех ключей в бакете через постраничный обход
    /// </summary>
    /// <returns>Список ключей объектов</returns>
    public async Task<List<string>> GetFileList()
    {
        var list = new List<string>();
        var request = new ListObjectsV2Request
        {
            BucketName = _bucketName,
            Prefix = "",
            Delimiter = ","
        };
        var paginator = client.Paginators.ListObjectsV2(request);

        logger.LogInformation("Получение списка файлов из бакета {Bucket}", _bucketName);
        await foreach (var response in paginator.Responses)
            if (response?.S3Objects != null)
                foreach (var obj in response.S3Objects)
                    if (obj != null)
                        list.Add(obj.Key);

        return list;
    }

    /// <summary>
    /// Загружает JSON-документ в бакет. Ключ объекта формируется как warehouse_{id}.json
    /// на основе поля id в исходном JSON
    /// </summary>
    /// <param name="fileData">Строковое представление JSON-документа</param>
    /// <returns>true если HTTP-статус загрузки = 200, иначе false</returns>
    /// <exception cref="ArgumentException">Если строка не валидный JSON или не содержит поля id</exception>
    public async Task<bool> UploadFile(string fileData)
    {
        var rootNode = JsonNode.Parse(fileData) ?? throw new ArgumentException("Строка не является валидным JSON");
        var id = rootNode["id"]?.GetValue<int>() ?? throw new ArgumentException("В JSON отсутствует поле 'id'");

        using var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, rootNode);
        stream.Seek(0, SeekOrigin.Begin);

        logger.LogInformation("Начата загрузка товара {Id} в бакет {Bucket}", id, _bucketName);
        var response = await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = $"warehouse_{id}.json",
            InputStream = stream
        });

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            logger.LogError("Не удалось загрузить товар {Id}: {Code}", id, response.HttpStatusCode);
            return false;
        }

        logger.LogInformation("Товар {Id} загружен в бакет {Bucket}", id, _bucketName);
        return true;
    }

    /// <summary>
    /// Скачивает файл из бакета и парсит его содержимое как JSON
    /// </summary>
    /// <param name="key">Ключ объекта в бакете</param>
    /// <returns>Распарсенный JsonNode</returns>
    /// <exception cref="InvalidOperationException">Если HTTP-статус не 200 или контент не валидный JSON</exception>
    public async Task<JsonNode> DownloadFile(string key)
    {
        logger.LogInformation("Начато скачивание файла {Key} из бакета {Bucket}", key, _bucketName);
        var response = await client.GetObjectAsync(new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        });

        if (response.HttpStatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException($"Не удалось скачать файл {key}: {response.HttpStatusCode}");

        using var reader = new StreamReader(response.ResponseStream, Encoding.UTF8);
        return JsonNode.Parse(reader.ReadToEnd()) ?? throw new InvalidOperationException($"Файл {key} не является валидным JSON");
    }

    /// <summary>
    /// Идемпотентно проверяет существование бакета и создаёт его при необходимости.
    /// Вызывается при старте приложения
    /// </summary>
    public async Task EnsureBucketExists()
    {
        logger.LogInformation("Проверка существования бакета {Bucket}", _bucketName);
        await client.EnsureBucketExistsAsync(_bucketName);
        logger.LogInformation("Бакет {Bucket} готов к работе", _bucketName);
    }
}
