using Amazon.S3;
using Amazon.SimpleNotificationService;
using Event.Sink.Messaging;
using Event.Sink.Storage;
using LocalStack.Client.Extensions;

namespace Event.Sink;

/// <summary>
/// Экстеншен для добавления различных служб в DI 
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
        return builder.AddSnsSubscriber();
    }

    /// <summary>
    /// Регистрирует службы для работы с SNS
    /// </summary>
    /// <param name="builder">Билдер</param>
    /// <returns>Билдер</returns>
    private static WebApplicationBuilder AddSnsSubscriber(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<SnsSubscriptionService>();
        builder.Services.AddAwsService<IAmazonSimpleNotificationService>();
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
        return builder.AddLocalstack();
    }

    /// <summary>
    /// Регистрирует службы для работы с S3 по классическому AWS API
    /// </summary>
    /// <param name="builder">Билдер</param>
    /// <returns>Билдер</returns>
    private static WebApplicationBuilder AddLocalstack(this WebApplicationBuilder builder)
    {
        builder.Services.AddAwsService<IAmazonS3>();
        builder.Services.AddScoped<IS3Service, S3AwsService>();
        return builder;
    }
}