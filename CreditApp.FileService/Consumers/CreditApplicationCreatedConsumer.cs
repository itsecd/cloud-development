using System.Text.Json;
using CreditApp.Application.Messages;
using CreditApp.FileService.Services;
using MassTransit;

namespace CreditApp.FileService.Consumers;

/// <summary>
/// Потребитель сообщений о создании кредитных заявок.
/// Получает сообщения из SQS-очереди (подписанной на SNS-топик), сериализует заявку в JSON
/// и сохраняет файл в объектное хранилище S3.
/// </summary>
/// <param name="fileStorage">Сервис файлового хранилища S3.</param>
/// <param name="logger">Логгер для записи событий.</param>
public class CreditApplicationCreatedConsumer(
    IS3FileStorage fileStorage,
    ILogger<CreditApplicationCreatedConsumer> logger) : IConsumer<CreditApplicationCreated>
{
    /// <summary>
    /// Обрабатывает входящее сообщение: сериализует кредитную заявку в JSON и загружает в S3.
    /// </summary>
    /// <param name="context">Контекст сообщения MassTransit.</param>
    public async Task Consume(ConsumeContext<CreditApplicationCreated> context)
    {
        var application = context.Message.Application;
        var key = $"credit-applications/{application.Id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";

        var json = JsonSerializer.Serialize(application);

        await fileStorage.UploadAsync(key, json, context.CancellationToken);

        logger.LogInformation("Saved credit application {Id} to S3 as {Key}", application.Id, key);
    }
}
