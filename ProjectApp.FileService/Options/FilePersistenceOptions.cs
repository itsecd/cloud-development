namespace ProjectApp.FileService.Options;

public class FilePersistenceOptions
{
    public const string SectionName = "FilePersistence";

    public string QueueName { get; set; } = "credit-application-generated";

    public string BucketName { get; set; } = "credit-applications";
}
