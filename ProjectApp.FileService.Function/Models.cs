using System.Text.Json.Serialization;

namespace ProjectApp.FileService.Function;

public class QueueRequest
{
    [JsonPropertyName("messages")]
    public List<QueueEvent> Messages { get; set; } = new();
}

public class QueueEvent
{
    [JsonPropertyName("details")]
    public QueueEventDetails? Details { get; set; }
}

public class QueueEventDetails
{
    [JsonPropertyName("message")]
    public QueueMessage? Message { get; set; }
}

public class QueueMessage
{
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}

public class CreditApplicationGeneratedEvent
{
    public int Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public CreditApplication Application { get; set; } = new();
}

public class CreditApplication
{
    public int Id { get; set; }
    public string CreditType { get; set; } = string.Empty;
    public decimal RequestedAmount { get; set; }
    public int TermMonths { get; set; }
    public double InterestRate { get; set; }
    public DateOnly ApplicationDate { get; set; }
    public bool RequiresInsurance { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly? DecisionDate { get; set; }
    public decimal? ApprovedAmount { get; set; }
}
