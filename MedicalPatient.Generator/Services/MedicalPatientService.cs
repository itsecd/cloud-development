using MedicalPatient.Generator.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;


namespace MedicalPatient.Generator.Services;

/// <summary>
/// </summary>
/// Сервис для работы с медицинскими пациентами<br/>
/// Каждый сгенерированный пациент сохраняется в кэш, если ранее не было пациента с таким ID <br/>
/// В ином случае значение берется из кэша
/// <param name="generator">Генератор случайных данных пациента</param>
/// <param name="cache">Кэш, хранящий ранее сгенерированных сотрудников</param>
/// <param name="logger">Логгер</param>
/// <param name="config">Конфигурация</param>
public class MedicalPatientService(
    MedicalPatientGenerator generator,
    IDistributedCache cache,
    ILogger<MedicalPatientService> logger,
    IConfiguration config
)
{
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(config.GetSection("CacheSetting").GetValue("CacheExpirationMinutes", 5));
    private const string CacheKeyPrefix = "medical-patient:";

    /// <summary>
    /// Функция для генерации или взятия из кэша сотрудника по id
    /// </summary>
    /// <param name="id">Id сотрудника</param>
    /// <param name="cancellationToken">Токен для отмены операции</param>
    /// <returns></returns>
    public async Task<MedicalPatientModel> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id), "Id must be greater than zero.");

        var cacheKey = $"{CacheKeyPrefix}{id}";

        logger.LogInformation("Запрос данных о пациенте с ID: {Id}", id);

        var cachedData = await cache.GetStringAsync(cacheKey, cancellationToken);

        if (!string.IsNullOrEmpty(cachedData))
        {
            logger.LogInformation("Медициенский пациент с {Id} найден в кеше", id);
            try
            {
                var cachedPatient = JsonSerializer.Deserialize<MedicalPatientModel>(cachedData);
                if (cachedPatient != null) return cachedPatient;
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Неверная структура JSON у пациента с ID {Id}", id);
            }
        }

        logger.LogInformation("Медициенский пациент с {Id} не найден в кеше, будет сгенерирован новый", id);

        var patient = generator.Generate(id);

        var serializedData = JsonSerializer.Serialize(patient);
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration
        };

        await cache.SetStringAsync(cacheKey, serializedData, cacheOptions, cancellationToken);

        logger.LogInformation(
            "Медициенский пациент с {Id} сохранен в кеш с TTL {CacheExpiration} м.",
            id,
            _cacheExpiration.TotalMinutes);

        return patient;
    }
}
