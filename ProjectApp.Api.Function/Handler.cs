using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Bogus;

namespace ProjectApp.Api.Function;

public class Handler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Faker<CreditApplication> Faker = CreateFaker();

    public FunctionResponse FunctionHandler(FunctionRequest request)
    {
        var id = TryReadId(request);
        if (id is null or <= 0)
        {
            return CreateResponse(400, """{"error":"id must be a positive integer"}""");
        }

        var application = Faker.Generate();
        application.Id = id.Value;

        PublishGeneratedAsync(application).GetAwaiter().GetResult();

        return CreateResponse(200, JsonSerializer.Serialize(application, JsonOptions));
    }

    private static int? TryReadId(FunctionRequest? request)
    {
        try
        {
            var rawId = TryGetValue(request?.QueryStringParameters, "id")
                ?? TryGetValue(request?.PathParameters, "id")
                ?? TryGetValue(request?.PathParams, "id");

            if (int.TryParse(rawId, out var id))
            {
                return id;
            }

            var path = request?.Path ?? request?.Url ?? string.Empty;
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
            {
                return ReadIdFromQuery(uri.Query);
            }

            var queryStart = path.IndexOf('?');
            if (queryStart >= 0)
            {
                return ReadIdFromQuery(path[queryStart..]);
            }
        }
        catch
        {
        }

        return null;
    }

    private static int? ReadIdFromQuery(string query)
    {
        var pairs = query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 &&
                parts[0].Equals("id", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(Uri.UnescapeDataString(parts[1]), out var id))
            {
                return id;
            }
        }

        return null;
    }

    private static string? TryGetValue(Dictionary<string, string>? values, string key)
    {
        if (values is null)
        {
            return null;
        }

        return values.TryGetValue(key, out var value) ? value : null;
    }

    private static async Task PublishGeneratedAsync(CreditApplication application)
    {
        var queueUrl = Environment.GetEnvironmentVariable("SQS_QUEUE_URL");
        var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

        if (string.IsNullOrWhiteSpace(queueUrl) ||
            string.IsNullOrWhiteSpace(accessKey) ||
            string.IsNullOrWhiteSpace(secretKey))
        {
            Console.WriteLine("[WARN] Message Queue settings are not configured");
            return;
        }

        var endpoint = Environment.GetEnvironmentVariable("SQS_ENDPOINT")
            ?? "https://message-queue.api.cloud.yandex.net";
        var region = Environment.GetEnvironmentVariable("YC_REGION") ?? "ru-central1";

        using var sqs = new AmazonSQSClient(
            new BasicAWSCredentials(accessKey, secretKey),
            new AmazonSQSConfig
            {
                ServiceURL = endpoint,
                AuthenticationRegion = region
            });

        var evt = new CreditApplicationGeneratedEvent
        {
            Id = application.Id,
            OccurredAtUtc = DateTime.UtcNow,
            Application = application
        };

        await sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = JsonSerializer.Serialize(evt, JsonOptions)
        });
    }

    private static FunctionResponse CreateResponse(int statusCode, string body)
        => new()
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["Access-Control-Allow-Origin"] = "*",
                ["Access-Control-Allow-Methods"] = "GET,OPTIONS",
                ["Access-Control-Allow-Headers"] = "Content-Type,Authorization"
            },
            Body = body
        };

    private static Faker<CreditApplication> CreateFaker()
    {
        var creditTypes = new[]
        {
            "Потребительский",
            "Ипотека",
            "Автокредит",
            "Рефинансирование",
            "Кредитная карта"
        };
        var nonTerminalStatuses = new[] { "Новая", "В обработке" };
        var terminalStatuses = new[] { "Одобрена", "Отклонена" };
        var minApplicationDate = DateTime.Today.AddYears(-2);
        var maxApplicationDate = DateTime.Today.AddDays(-1);

        return new Faker<CreditApplication>("ru")
            .RuleFor(c => c.CreditType, f => f.PickRandom(creditTypes))
            .RuleFor(c => c.RequestedAmount, f => Math.Round(f.Finance.Amount(50000, 5000000), 2))
            .RuleFor(c => c.TermMonths, f => f.Random.Int(6, 360))
            .RuleFor(c => c.InterestRate, f => Math.Round(f.Random.Double(21.0, 33.0), 2))
            .RuleFor(c => c.ApplicationDate, f => DateOnly.FromDateTime(f.Date.Between(minApplicationDate, maxApplicationDate)))
            .RuleFor(c => c.RequiresInsurance, f => f.Random.Bool())
            .RuleFor(c => c.Status, f => f.Random.Bool(0.7f)
                ? f.PickRandom(terminalStatuses)
                : f.PickRandom(nonTerminalStatuses))
            .RuleFor(c => c.DecisionDate, (f, c) =>
            {
                if (c.Status is not ("Одобрена" or "Отклонена"))
                {
                    return null;
                }

                var minDate = c.ApplicationDate.ToDateTime(TimeOnly.MinValue).AddDays(1);
                var maxDate = DateTime.Today;
                return DateOnly.FromDateTime(f.Date.Between(minDate, maxDate));
            })
            .RuleFor(c => c.ApprovedAmount, (f, c) =>
            {
                if (c.Status != "Одобрена")
                {
                    return null;
                }

                return Math.Round(f.Finance.Amount(50000, c.RequestedAmount), 2);
            });
    }
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

public class CreditApplicationGeneratedEvent
{
    public int Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public CreditApplication Application { get; set; } = new();
}

public class FunctionRequest
{
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("queryStringParameters")]
    public Dictionary<string, string>? QueryStringParameters { get; set; }

    [JsonPropertyName("pathParameters")]
    public Dictionary<string, string>? PathParameters { get; set; }

    [JsonPropertyName("pathParams")]
    public Dictionary<string, string>? PathParams { get; set; }
}

public class FunctionResponse
{
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    [JsonPropertyName("headers")]
    public Dictionary<string, string> Headers { get; set; } = new();

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("isBase64Encoded")]
    public bool IsBase64Encoded { get; set; }
}
