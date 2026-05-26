namespace ProjectApp.FileService.ObjectStorage;

public sealed class ObjectStorageOptions
{
    public const string SectionName = "ObjectStorage";

    public string Endpoint { get; init; } = "localhost:9000";

    public string AccessKey { get; init; } = "minioadmin";

    public string SecretKey { get; init; } = "minioadmin";

    public string BucketName { get; init; } = "generated-data";

    public bool UseSsl { get; init; }
}
