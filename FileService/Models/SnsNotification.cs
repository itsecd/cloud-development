namespace FileService.Models;

public class SnsNotification
{
    public string Type { get; set; } = string.Empty;
    public string? MessageId { get; set; }
    public string? TopicArn { get; set; }
    public string? Subject { get; set; }
    public string? Message { get; set; }
    public string? Timestamp { get; set; }
    public string? SubscribeURL { get; set; }
    public string? Token { get; set; }
    public string? UnsubscribeURL { get; set; }
}
