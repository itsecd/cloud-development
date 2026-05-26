using ProjectApp.Domain.Messaging;

namespace ProjectApp.FileService.ObjectStorage;

public interface IPatientFileStorage
{
    Task SaveAsync(PatientGeneratedMessage message, CancellationToken cancellationToken = default);

    Task<string?> GetPatientJsonAsync(int patientId, CancellationToken cancellationToken = default);
}
