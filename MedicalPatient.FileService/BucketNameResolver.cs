namespace MedicalPatient.FileService;

public static class BucketNameResolver
{
    private const string DefaultBucketName = "medical-patient";
    private const string CommonTypoBucketName = "medical-patinet";

    public static string Resolve(string? configuredBucketName)
    {
        if (string.IsNullOrWhiteSpace(configuredBucketName))
            return DefaultBucketName;

        if (string.Equals(configuredBucketName, CommonTypoBucketName, StringComparison.OrdinalIgnoreCase))
            return DefaultBucketName;

        return configuredBucketName;
    }
}
