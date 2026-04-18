using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Service.Api.Entity;

namespace Service.Api.Broker;

public class SnsPublisherService(IAmazonSimpleNotificationService client, IConfiguration configuration, ILogger<SnsPublisherService> logger) : IProducerService
{
    private readonly string _topicArn = configuration["AWS:Resources:SNSTopicArn"]
        ?? throw new KeyNotFoundException("SNS topic link was not found in configuration");
    public async Task SendMessage(ProgramProject pp)
    {
        try
        {
            var json = JsonSerializer.Serialize(pp);
            var request = new PublishRequest
            {
                Message = json,
                TopicArn = _topicArn
            };
            var response = await client.PublishAsync(request);
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK) logger.LogInformation($"Programproj {pp.Id} was sent to storage via SNS");
            else throw new Exception($"SNS returned {response.HttpStatusCode}");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unable to send programproj through sns topic");
        }
    }
}
