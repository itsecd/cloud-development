using Cloud.FileServiceFunction.Models;
using Cloud.FileServiceFunction.Services;
using System.Text.Json;

namespace Cloud.FileServiceFunction;

/// <summary>
/// Точка входа облачной функции файлового сервиса
/// </summary>
public class Handler
{
    private static readonly S3StorageService _storageService = new();

    /// <summary>
    /// Обрабатывает вызов функции, инициированный триггером Message Queue
    /// </summary>
    /// <param name="request">Данные из очереди</param>
    /// <returns>Объект с количеством обработанных сообщений</returns>
    public object FunctionHandler(QueueRequest request)
    {
        var processed = _storageService.ProcessMessagesAsync(request).GetAwaiter().GetResult();
        return new
        {
            statusCode = 200,
            body = JsonSerializer.Serialize(new { processed })
        };
    }
}
