using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using CompanyEmployee.DtoModel;
using System.Text.Json;

namespace CompanyEmployee.ApiService.Messaging;

public class SnsPublisher(IAmazonSimpleNotificationService snsClient)
{
    private readonly string _topicName = "companyemployee-topic";

    private string? _topicArn;

    private async Task<string> GetTopicArn()
    {
        if (_topicArn != null)
            return _topicArn;

        var response = await snsClient.CreateTopicAsync(new CreateTopicRequest
        {
            Name = _topicName
        });

        _topicArn = response.TopicArn;

        return _topicArn;
    }

    public async Task Publish(ModelDTO dto)
    {
        var topicArn = await GetTopicArn();

        var message = JsonSerializer.Serialize(dto);

        await snsClient.PublishAsync(new PublishRequest
        {
            TopicArn = topicArn,
            Message = message
        });
    }
}