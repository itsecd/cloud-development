using Amazon.SQS;
using Service.Storage.Messaging;
using Service.Storage.Storage;
using LocalStack.Client.Extensions;

namespace Service.Storage;

/// <summary>
/// Экстеншен для добавления различных служб в DI в зависимости от конфигурации приложения
/// </summary>
internal static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Регистрирует клиентские службы для работы с брокером сообщений
    /// </summary>
    /// <param name="builder">Билдер</param>
    /// <returns>Билдер</returns>
    /// <exception cref="KeyNotFoundException">Если настройки не найдены</exception>
    public static WebApplicationBuilder AddConsumer(this WebApplicationBuilder builder)
    {
        builder.Services.AddLocalStack(builder.Configuration);
        builder.Services.AddHostedService<SqsConsumerService>();
        builder.Services.AddAwsService<IAmazonSQS>();
        return builder;
    }

    /// <summary>
    /// Регистрирует клиентские службы для работы с объектным хранилищем
    /// </summary>
    /// <param name="builder">Билдер</param>
    /// <returns>Билдер</returns>
    /// <exception cref="KeyNotFoundException">Если настройки не найдены</exception>
    public static WebApplicationBuilder AddS3(this WebApplicationBuilder builder)
    {
        builder.AddMinioClient("credit-order-minio");
        builder.Services.AddScoped<S3MinioService>();
        return builder;
    }
}
