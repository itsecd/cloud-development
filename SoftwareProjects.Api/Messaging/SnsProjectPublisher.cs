using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using SoftwareProjects.Api.Entities;
using System.Net;
using System.Text.Json;

namespace SoftwareProjects.Api.Messaging;

/// <summary>
/// Реализация <see cref="IProjectPublisher"/>, публикующая сообщение в SNS-топик AWS/LocalStack
/// </summary>
/// <param name="snsClient">Клиент Amazon SNS, разрешённый из DI</param>
/// <param name="configuration">Конфигурация приложения; используется для получения ARN целевого топика</param>
/// <param name="logger">Структурный логгер</param>
public class SnsProjectPublisher(
    IAmazonSimpleNotificationService snsClient,
    IConfiguration configuration,
    ILogger<SnsProjectPublisher> logger) : IProjectPublisher
{
    /// <summary>
    /// ARN SNS-топика, в который публикуются сообщения. Берётся из CloudFormation outputs (<c>AWS:Resources:SNSTopicArn</c>)
    /// </summary>
    private readonly string _topicArn = configuration["AWS:Resources:SNSTopicArn"]
        ?? throw new KeyNotFoundException("SNS topic ARN was not found in configuration");

    /// <summary>
    /// Сериализует переданный программный проект в JSON и отправляет его в SNS-топик
    /// </summary>
    /// <param name="project">Программный проект для публикации</param>
    /// <returns>Задача, завершающаяся после получения ответа от SNS</returns>
    public async Task Publish(SoftwareProject project)
    {
        try
        {
            var json = JsonSerializer.Serialize(project);
            var request = new PublishRequest
            {
                Message = json,
                TopicArn = _topicArn
            };
            var response = await snsClient.PublishAsync(request);
            if (response.HttpStatusCode == HttpStatusCode.OK)
                logger.LogInformation("Software project {ProjectId} was published to SNS topic", project.Id);
            else
                throw new Exception($"SNS returned {response.HttpStatusCode}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish software project {ProjectId} to SNS", project.Id);
        }
    }
}
