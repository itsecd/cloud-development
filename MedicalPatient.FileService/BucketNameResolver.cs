namespace MedicalPatient.FileService;

public static class BucketNameResolver
{
    private const string DefaultBucketName = "medical-patient";
    private const string CommonTypoBucketName = "medical-patinet";

    public static string Resolve(string? configuredBucketName) =>
        configuredBucketName switch
        {
            null or "" => DefaultBucketName,
            string name when name.Equals(CommonTypoBucketName, StringComparison.OrdinalIgnoreCase) => DefaultBucketName,
            _ => configuredBucketName
        };
}