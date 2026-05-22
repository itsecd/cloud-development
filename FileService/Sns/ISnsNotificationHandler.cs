namespace File.Service.Sns;

/// <summary>
/// Обрабатывает входящие SNS-уведомления, поступающие в HTTP-эндпойнт файлового сервиса.
/// </summary>
public interface ISnsNotificationHandler
{
    /// <summary>
    /// Разбирает SNS-сообщение из текущего HTTP-запроса и выполняет его обработку:
    /// либо подтверждает подписку, либо сохраняет полезную нагрузку в объектное хранилище.
    /// </summary>
    Task<IResult> HandleAsync(HttpContext context, CancellationToken cancellationToken);
}
