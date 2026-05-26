using ProjectApp.Domain.Entities;

namespace ProjectApp.Domain.Messaging;

public sealed class PatientGeneratedMessage
{
    public required MedicalPatient Patient { get; init; }

    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
}
