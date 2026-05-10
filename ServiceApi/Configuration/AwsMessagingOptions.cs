namespace Service.Api.Configuration;

/// <summary>
/// Параметры интеграции с AWS-совместимыми сервисами LocalStack.
/// </summary>
public sealed class AwsMessagingOptions
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
}
