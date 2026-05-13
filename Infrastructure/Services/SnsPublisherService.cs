using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Domain.Contracts;
using Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Services;

public class SnsPublisherService(
    IAmazonSimpleNotificationService snsClient,
    IConfiguration configuration,
    ILogger<SnsPublisherService> logger) : ISnsPublisherService
{
    private readonly string _topicName = configuration["AWS:TopicName"] ?? "vehicle-contracts";

    public async Task PublishVehicleContractAsync(VehicleContractDto contract, CancellationToken ct = default)
    {
        var topicArn = await GetOrCreateTopicArnAsync(ct);
        var message = JsonSerializer.Serialize(contract);

        await snsClient.PublishAsync(new PublishRequest
        {
            TopicArn = topicArn,
            Message = message,
            Subject = "VehicleContract"
        }, ct);

        logger.LogInformation("Published vehicle contract {SystemId} to SNS topic {TopicArn}",
            contract.SystemId, topicArn);
    }

    private async Task<string> GetOrCreateTopicArnAsync(CancellationToken ct)
    {
        var response = await snsClient.CreateTopicAsync(new CreateTopicRequest
        {
            Name = _topicName
        }, ct);
        return response.TopicArn;
    }
}
