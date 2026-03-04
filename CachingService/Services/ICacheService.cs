using Domain.Entities;

namespace CachingService.Services;

public interface ICacheService
{
   public Task<MedicalPatient?> RetriveFromCache(int id);

   public Task PutInCache(MedicalPatient patient);
}
