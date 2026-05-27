namespace ProjectApp.Domain.Messaging;

public sealed class PatientMessagingOptions
{
    public const string SectionName = "PatientMessaging";

    public string TopicName { get; init; } = "patients-generated";

    public string QueueName { get; init; } = "patients-generated-files";

    public string Region { get; init; } = "us-east-1";

    public string? ServiceUrl { get; init; } = "http://localhost:4566";

    public string AccessKey { get; init; } = "test";

    public string SecretKey { get; init; } = "test";

    public int WaitTimeSeconds { get; init; } = 10;
}
