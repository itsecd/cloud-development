using Microsoft.AspNetCore.Builder;
using Service.Storage.Storage;
using Service.Storage.Messaging;

namespace Service.Storage;

/// <summary>
/// Экстеншен для добавления брокера в зависимости от конфигурации приложения
/// </summary>
internal static class WebApplicationExtensions
{
    /// <summary>
    /// Конфигурирует клиенские службы для взаимодействия с S3
    /// </summary>
    /// <param name="app">Билдер</param>
    /// <returns>Билдер</returns>
    public static async Task<WebApplication> UseS3(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var s3Service = scope.ServiceProvider.GetRequiredService<S3MinioService>();
        await s3Service.EnsureBucketExists();
        return app;
    }
}
