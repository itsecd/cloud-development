using System.Text.Json;

namespace File.Service.Storage;

public interface IVehicleStorageService
{
    /// <summary>
    /// Создаёт бакет в S3, если его не существует
    /// </summary>
    Task PrepareBucketAsync();

    /// <summary>
    /// Сохраняет JSON-данные автомобиля в файл
    /// </summary>
    /// <param name="jsonData">JSON с данными автомобиля</param>
    /// <returns>true если успешно сохранено</returns>
    Task<bool> StoreVehicleDataAsync(string jsonData);

    /// <summary>
    /// Возвращает список всех ключей (имён файлов) в бакете
    /// </summary>
    Task<List<string>> GetAllFileKeysAsync();

    /// <summary>
    /// Скачивает и возвращает JSON по ключу файла
    /// </summary>
    Task<JsonDocument?> FetchVehicleFileAsync(string fileKey);

    /// <summary>
    /// Формирует имя файла по ID автомобиля
    /// </summary>
    static string BuildFileKey(int vehicleId) => $"car_{vehicleId}.json";
}