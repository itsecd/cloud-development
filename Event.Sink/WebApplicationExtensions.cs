using Event.Sink.Messaging;
using Event.Sink.Storage;

namespace Event.Sink;

/// <summary>
/// Экстеншен для добавления брокера
/// </summary>
internal static class WebApplicationExtensions
{
    /// <summary>
    /// Конфигурирует клиенские службы для взаимодействия с брокером сообщений
    /// </summary>
    /// <param name="app">Билдер</param>
    /// <returns>Билдер</returns>
    /// <exception cref="KeyNotFoundException">Если настройки не найдены</exception>
    public static async Task<WebApplication> UseConsumer(this WebApplication app)
    {
        return await app.UseSnsSubscriber();
    }

    /// <summary>
    /// Запускает службу для подписки на SNS
    /// </summary>
    /// <param name="app">Билдер</param>
    /// <returns>Билдер</returns>
    private static async Task<WebApplication> UseSnsSubscriber(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var subscriptionService = scope.ServiceProvider.GetRequiredService<SnsSubscriptionService>();
        await subscriptionService.SubscribeEndpoint();
        return app;
    }

    /// <summary>
    /// Конфигурирует клиенские службы для взаимодействия с S3
    /// </summary>
    /// <param name="app">Билдер</param>
    /// <returns>Билдер</returns>
    public static async Task<WebApplication> UseS3(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var s3Service = scope.ServiceProvider.GetRequiredService<IS3Service>();
        await s3Service.EnsureBucketExists();
        return app;
    }
}