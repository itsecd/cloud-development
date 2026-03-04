using CachingService.Services;
using Domain.Entities;

namespace GenerationService.Services;

public class GeneratorService(ICacheService cacheService, ILogger logger) : IGeneratorService
{
    public async Task<MedicalPatient> GenerateAsync(int id)
    {
        MedicalPatient? cachedPatient = null;

        try
        {
            cachedPatient = await cacheService.RetriveFromCache(id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache read failed for Id {Id}.", id);
        }

        if (cachedPatient is not null)
        {
            logger.LogInformation("Patient with Id {Id} retrieved from cache.", id);

            return cachedPatient;
        }

        MedicalPatient patient;
        try
        {
            patient = Generator.GenerateAsync(id);
            logger.LogInformation("Patient with Id {Id} generated.", id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Patient generation failed for Id {Id}", id);
            throw;
        }

        try
        {
            await cacheService.PutInCache(patient);
            logger.LogInformation("Patient with Id {Id} stored in cache.", id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache write failed for Id {Id}", id);
        }

        return patient;
    }
}
