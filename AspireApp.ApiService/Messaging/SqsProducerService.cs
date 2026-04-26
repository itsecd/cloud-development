using Amazon.SQS;
using AspireApp.ApiService.Entities;
using System.Net;
using System.Text.Json;

namespace AspireApp.ApiService.Messaging;

/// <summary>
/// Служба-публикатор сообщений в SQS. Сериализует объект Warehouse в JSON
/// и отправляет в очередь, имя которой берётся из конфигурации
/// </summary>
/// <param name="client">AWS SDK клиент SQS</param>
/// <param name="configuration">Конфигурация приложения, содержит имя очереди</param>
/// <param name="logger">Логгер</param>
public class SqsProducerService(IAmazonSQS client, IConfiguration configuration, ILogger<SqsProducerService> logger)
{
    private readonly string _queueName = configuration["AWS:Resources:SQSQueueName"]
        ?? throw new KeyNotFoundException("Имя SQS очереди не найдено в конфигурации");

    /// <summary>
    /// Отправляет сериализованный Warehouse в SQS-очередь.
    /// Исключения логируются и не пробрасываются — endpoint остаётся доступным
    /// даже при сбое брокера
    /// </summary>
    /// <param name="warehouse">Сгенерированный товар на складе</param>
    public async Task SendMessage(Warehouse warehouse)
    {
        try
        {
            var json = JsonSerializer.Serialize(warehouse);
            var response = await client.SendMessageAsync(_queueName, json);
            if (response.HttpStatusCode == HttpStatusCode.OK)
                logger.LogInformation("Товар {Id} отправлен в SQS", warehouse.Id);
            else
                throw new Exception($"SQS вернул статус {response.HttpStatusCode}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось отправить товар {Id} в SQS", warehouse.Id);
        }
    }
}
