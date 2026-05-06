using System.Net;
using System.Text.Json;
using Amazon.SQS;
using CourseApp.Api.Models;

namespace CourseApp.Api.Messaging;

/// <summary>
/// Реализация продюсера на базе AWS SQS
/// </summary>
/// <param name="client">Клиент SQS, поднятый AWS SDK через LocalStack</param>
/// <param name="configuration">Конфигурация (имя очереди берётся из CloudFormation Outputs)</param>
/// <param name="logger">Логгер</param>
public sealed class SqsProducerService(
    IAmazonSQS client,
    IConfiguration configuration,
    ILogger<SqsProducerService> logger) : IProducerService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _queueName = configuration["AWS:Resources:SQSQueueName"]
        ?? throw new KeyNotFoundException("SQS queue name was not found in configuration");

    /// <inheritdoc/>
    public async Task SendMessage(Course course)
    {
        try
        {
            var json = JsonSerializer.Serialize(course, _jsonOptions);
            var response = await client.SendMessageAsync(_queueName, json);
            if (response.HttpStatusCode == HttpStatusCode.OK)
                logger.LogInformation("Course {Id} was sent to SQS", course.Id);
            else
                throw new InvalidOperationException($"SQS returned {response.HttpStatusCode}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to send course {Id} through SQS", course.Id);
        }
    }
}
