using Amazon.SimpleNotificationService;
using LocalStack.Client.Extensions;
using Service.Api.Messaging;

namespace Service.Api;

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
    public static WebApplicationBuilder AddProducer(this WebApplicationBuilder builder)
    {
        builder.Services.AddLocalStack(builder.Configuration);
        return builder.AddSnsPublisher();
    }

    /// <summary>
    /// Регистрирует службы для работы с SNS
    /// </summary>
    /// <param name="builder">Билдер</param>
    /// <returns>Билдер</returns>
    private static WebApplicationBuilder AddSnsPublisher(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IProducerService, SnsPublisherService>();
        builder.Services.AddAwsService<IAmazonSimpleNotificationService>();
        return builder;
    }

}