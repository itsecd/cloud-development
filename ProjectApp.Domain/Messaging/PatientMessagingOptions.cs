namespace ProjectApp.Domain.Messaging;

public sealed class PatientMessagingOptions
{
    public const string SectionName = "PatientMessaging";

    public string ExchangeName { get; init; } = "patients.generated";

    public string QueueName { get; init; } = "patients.generated.files";

    public string RoutingKey { get; init; } = "patient.generated";
}
