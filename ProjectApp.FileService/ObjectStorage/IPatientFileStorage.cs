using ProjectApp.Domain.Messaging;

namespace ProjectApp.FileService.ObjectStorage;

public interface IPatientFileStorage
{
    public Task SaveAsync(PatientGeneratedMessage message, CancellationToken cancellationToken = default);

    public Task<string?> GetPatientJsonAsync(int patientId, CancellationToken cancellationToken = default);
}
