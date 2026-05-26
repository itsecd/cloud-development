using ProjectApp.Domain.Entities;

namespace ProjectApp.Api.Services.Messaging;

public interface IPatientGeneratedPublisher
{
    public Task PublishAsync(MedicalPatient patient, CancellationToken cancellationToken = default);
}
