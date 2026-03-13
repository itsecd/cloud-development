using Microsoft.Extensions.Caching.Distributed;
using ProjectApp.Domain.Entities;
using System.Text.Json;

namespace ProjectApp.Api.Services.ProjectGeneratorService;

/// <summary>
/// Сервис получения медицинского пациента: сначала ищет в кэше, при промахе генерирует нового и сохраняет
/// </summary>
public class MedicalPatientGeneratorService(
    IDistributedCache cache,
    MedicalPatientGenerator generator,
    IConfiguration configuration,
    ILogger<MedicalPatientGeneratorService> logger) : IMedicalPatientGeneratorService
{
    private readonly int _expirationMinutes = configuration.GetValue("CacheSettings:ExpirationMinutes", 10);

    /// <summary>
    /// Возвращает медицинского пациента по идентификатору.
    /// Если пациент найден в кэше, возвращается из него; иначе генерируется, сохраняется в кэш и возвращается.
    /// </summary>
    public async Task<MedicalPatient> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Attempting to retrieve medical patient {Id} from cache", id);

        var cacheKey = $"medical-patient-{id}";

        MedicalPatient? patient = null;
        try
        {
            var cachedData = await cache.GetStringAsync(cacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(cachedData))
            {
                patient = JsonSerializer.Deserialize<MedicalPatient>(cachedData);

                if (patient != null)
                {
                    logger.LogInformation("Medical patient {Id} found in cache", id);
                    return patient;
                }

                logger.LogWarning("Patient {Id} was found in cache but could not be deserialized. Generating a new one", id);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to retrieve patient {Id} from cache (error ignored)", id);
        }

        logger.LogInformation("Patient {Id} not found in cache or cache unavailable, generating a new one", id);
        patient = generator.Generate();
        patient.Id = id;

        try
        {
            logger.LogInformation("Saving patient {Id} to cache", id);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_expirationMinutes)
            };

            await cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(patient),
                cacheOptions,
                cancellationToken);

            logger.LogInformation(
                "Medical patient generated and cached: Id={Id}, FullName={FullName}, BirthDate={BirthDate}, BloodGroup={BloodGroup}, RhFactor={RhFactor}",
                patient.Id,
                patient.FullName,
                patient.BirthDate,
                patient.BloodGroup,
                patient.RhFactor);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save patient {Id} to cache (error ignored)", id);
        }

        return patient;
    }
}
