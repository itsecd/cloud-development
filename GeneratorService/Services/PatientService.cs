using System.Text.Json;
using GeneratorService.Generators;
using GeneratorService.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace GeneratorService.Services;

public sealed class PatientService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<PatientService> _logger;

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public PatientService(IDistributedCache cache, ILogger<PatientService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<MedicalPatient> GetAsync(int id, CancellationToken ct = default)
    {
        var key = $"patient:{id}";

        var cached = await _cache.GetStringAsync(key, ct);
        if (cached is not null)
        {
            _logger.LogInformation("Cache HIT  | id={Id}", id);
            return JsonSerializer.Deserialize<MedicalPatient>(cached)!;
        }

        _logger.LogInformation("Cache MISS | id={Id} — generating", id);

        var patient = MedicalPatientGenerator.Generate(id);
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(patient), CacheOptions, ct);

        _logger.LogInformation(
            "Generated  | id={Id} Name={FullName} BirthDate={BirthDate} BloodGroup={BloodGroup} RhFactor={RhFactor}",
            patient.Id, patient.FullName, patient.BirthDate, patient.BloodGroup, patient.RhFactor);

        return patient;
    }
}
