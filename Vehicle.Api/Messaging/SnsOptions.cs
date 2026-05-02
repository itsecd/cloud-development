namespace Vehicle.Api.Messaging;

/// <summary>
/// Настройки SNS для отправки сообщений в брокер.
/// </summary>
public class SnsOptions
{
    /// <summary>
    /// ARN SNS topic.
    /// </summary>
    public string TopicArn { get; set; } = string.Empty;
}