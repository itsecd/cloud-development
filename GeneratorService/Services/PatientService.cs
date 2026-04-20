using System.Text.Json;
using GeneratorService.Generators;
using GeneratorService.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace GeneratorService.Services;

public sealed class PatientService(
    IDistributedCache cache,
    ILogger<PatientService> logger,
    IConfiguration configuration,
    SnsPublisherService snsPublisher)
{
    public async Task<MedicalPatient> GetAsync(int id, CancellationToken ct = default)
    {
        var key = $"patient:{id}";

        var cached = await cache.GetStringAsync(key, ct);
        if (cached is not null)
        {
            var cachedPatient = JsonSerializer.Deserialize<MedicalPatient>(cached);
            if (cachedPatient is not null)
            {
                logger.LogInformation("Cache HIT  | id={Id}", id);
                return cachedPatient;
            }
        }

        logger.LogInformation("Cache MISS | id={Id} — generating", id);

        var patient = MedicalPatientGenerator.Generate(id);
        var patientJson = JsonSerializer.Serialize(patient);

        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(
                configuration.GetValue<double>("CacheSettings:AbsoluteExpirationMinutes"))
        };

        await cache.SetStringAsync(key, patientJson, cacheOptions, ct);

        logger.LogInformation(
            "Generated  | id={Id} Name={FullName} BirthDate={BirthDate} BloodGroup={BloodGroup} RhFactor={RhFactor}",
            patient.Id, patient.FullName, patient.BirthDate, patient.BloodGroup, patient.RhFactor);

        try
        {
            await snsPublisher.PublishAsync(patientJson, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish patient {Id} to SNS", id);
        }

        return patient;
    }
}