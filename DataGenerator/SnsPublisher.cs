using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Contracts;

namespace DataGenerator;

public class SnsPublisher
{
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly ILogger<SnsPublisher> _logger;
    private readonly string _topicArn;

    public SnsPublisher(
        IAmazonSimpleNotificationService snsClient,
        ILogger<SnsPublisher> logger,
        IConfiguration configuration)
    {
        _snsClient = snsClient;
        _logger = logger;
        _topicArn = configuration["Sns:TopicArn"]
            ?? "arn:aws:sns:us-east-1:000000000000:employee-topic";
    }

    public async Task PublishAsync(List<Employee> employees)
    {
        var message = new EmployeeMessage
        {
            Action = "generated",
            Employees = employees
        };

        var json = JsonSerializer.Serialize(message);

        try
        {
            var response = await _snsClient.PublishAsync(new PublishRequest
            {
                TopicArn = _topicArn,
                Message = json
            });

            _logger.LogInformation(
                "Published {Count} employees to SNS. MessageId: {MessageId}",
                employees.Count, response.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish to SNS");
            throw;
        }
    }
}
