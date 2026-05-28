using System.Text.Json;

namespace File.Service.Storage;

/// <summary>
/// Интерфейс сервиса для работы с хранилищем файлов в S3
/// </summary>
public interface IVehicleStorageService
{
    /// <summary>
    /// Создаёт бакет в S3, если его не существует
    /// </summary>
    public Task PrepareBucketAsync();

    /// <summary>
    /// Сохраняет JSON-данные автомобиля в файл
    /// </summary>
    /// <param name="jsonData">JSON с данными автомобиля</param>
    /// <returns>true если успешно сохранено</returns>
    public Task<bool> StoreVehicleDataAsync(string jsonData);

    /// <summary>
    /// Возвращает список всех ключей (имён файлов) в бакете
    /// </summary>
    public Task<List<string>> GetAllFileKeysAsync();

    /// <summary>
    /// Скачивает и возвращает JSON по ключу файла
    /// </summary>
    public Task<JsonDocument?> FetchVehicleFileAsync(string fileKey);

    /// <summary>
    /// Формирует имя файла по ID автомобиля
    /// </summary>
    static string BuildFileKey(int vehicleId) => $"car_{vehicleId}.json";
}