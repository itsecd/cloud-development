namespace ProjectApp.Api.Options;

/// <summary>
/// Настройки брокера сообщений для публикации событий о генерации заявок.
/// </summary>
public class AwsMessagingOptions
{
    /// <summary>
    /// Название секции в appsettings.
    /// </summary>
    public const string SectionName = "AwsMessaging";

    /// <summary>
    /// Имя SQS очереди.
    /// </summary>
    public string QueueName { get; set; } = "credit-application-generated";
}
