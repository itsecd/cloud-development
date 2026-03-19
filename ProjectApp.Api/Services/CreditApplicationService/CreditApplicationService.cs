using ProjectApp.Domain.Entities;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;

namespace ProjectApp.Api.Services.CreditApplicationService;

/// <summary>
/// Сервис получения кредитной заявки с использованием SQS для кэширования
/// </summary>
public class CreditApplicationService(
    IAmazonSQS sqsClient,
    CreditApplicationGenerator generator,
    IConfiguration configuration,
    ILogger<CreditApplicationService> logger) : ICreditApplicationService
{
    private readonly string _queueUrl = configuration["SQS:QueueUrl"] ?? "http://localhost:4566/000000000000/credit-application-cache";

    /// <summary>
    /// Возвращает кредитную заявку по идентификатору.
    /// Если заявка найдена в SQS — возвращается из него; иначе генерируется, сохраняется в SQS и возвращается.
    /// </summary>
    /// <param name="id">Идентификатор заявки</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Кредитная заявка</returns>
    public async Task<CreditApplication> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Attempting to retrieve credit application {Id} from SQS", id);

        // Получаем сообщения из SQS
        CreditApplication? application = null;
        try
        {
            var receiveRequest = new ReceiveMessageRequest
            {
                QueueUrl = _queueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 0
            };

            var response = await sqsClient.ReceiveMessageAsync(receiveRequest, cancellationToken);

            foreach (var message in response.Messages)
            {
                try
                {
                    var app = JsonSerializer.Deserialize<CreditApplication>(message.Body);
                    if (app != null && app.Id == id)
                    {
                        logger.LogInformation("Credit application {Id} found in SQS", id);
                        
                        // Удаляем сообщение после получения
                        await sqsClient.DeleteMessageAsync(new DeleteMessageRequest
                        {
                            QueueUrl = _queueUrl,
                            ReceiptHandle = message.ReceiptHandle
                        }, cancellationToken);
                        
                        return app;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to deserialize message from SQS");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to receive messages from SQS (error ignored)");
        }

        // Если в SQS нет или ошибка — генерируем новую заявку
        logger.LogInformation("Credit application {Id} not found in SQS or SQS unavailable, generating a new one", id);
        application = generator.Generate();
        application.Id = id;

        // Попытка сохранить в SQS
        try
        {
            logger.LogInformation("Saving credit application {Id} to SQS", id);

            var sendRequest = new SendMessageRequest
            {
                QueueUrl = _queueUrl,
                MessageBody = JsonSerializer.Serialize(application),
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    { "ApplicationId", new MessageAttributeValue { StringValue = id.ToString(), DataType = "String" } }
                }
            };

            await sqsClient.SendMessageAsync(sendRequest, cancellationToken);

            logger.LogInformation(
                "Credit application generated and sent to SQS: Id={Id}, Client={ClientFullName}, Amount={CreditAmount}, Purpose={CreditPurpose}, Score={CreditScore}",
                application.Id,
                application.ClientFullName,
                application.CreditAmount,
                application.CreditPurpose,
                application.CreditScore);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send credit application {Id} to SQS (error ignored)", id);
        }

        return application;
    }
}
