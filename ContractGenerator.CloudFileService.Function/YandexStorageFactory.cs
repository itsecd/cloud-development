using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Configuration;

namespace ContractGenerator.CloudFileService.Function;

internal static class YandexStorageFactory
{
    public static IAmazonS3 CreateClient(IConfiguration configuration) =>
        new AmazonS3Client(
            new BasicAWSCredentials(Required(configuration, "YC_STATIC_KEY_ID"), Required(configuration, "YC_STATIC_KEY_SECRET")),
            new AmazonS3Config
            {
                ServiceURL = configuration["YC_S3_ENDPOINT"] ?? "https://storage.yandexcloud.net",
                ForcePathStyle = true
            });

    public static string GetBucketName(IConfiguration configuration) =>
        Required(configuration, "YC_FILES_BUCKET");

    private static string Required(IConfiguration configuration, string key) =>
        configuration[key] ?? throw new InvalidOperationException($"{key} is not configured");
}
