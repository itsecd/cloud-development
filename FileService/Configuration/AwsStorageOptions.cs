namespace File.Service.Configuration;

/// <summary>
/// Настройки интеграции с LocalStack для хранения файлов и чтения сообщений.
/// </summary>
public sealed class AwsStorageOptions
{
    public const string SectionName = "Aws";

    /// <summary>
    /// Базовый URL LocalStack.
    /// </summary>
    public string ServiceUrl { get; set; } = "http://localhost:4566";

    /// <summary>
    /// Регион AWS.
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Access key для LocalStack.
    /// </summary>
    public string AccessKey { get; set; } = "test";

    /// <summary>
    /// Secret key для LocalStack.
    /// </summary>
    public string SecretKey { get; set; } = "test";

    /// <summary>
    /// Имя SNS-топика с событиями генерации сотрудников.
    /// </summary>
    public string TopicName { get; set; } = "employee-generated-topic";

    /// <summary>
    /// Имя SQS-очереди, подписанной на SNS-топик.
    /// </summary>
    public string QueueName { get; set; } = "employee-generated-queue";

    /// <summary>
    /// Имя S3-бакета для файлов сотрудников.
    /// </summary>
    public string BucketName { get; set; } = "employee-files";
}
