using CreditApp.Domain.Entities;
using System.Text.Json;

namespace CreditApp.Api.Services.CreditApplicationService;

/// <summary>
/// Сервис работы с файловым хранилищем через FileService API
/// </summary>
public class CreditApplicationStorageService(
    IHttpClientFactory httpClientFactory,
    ILogger<CreditApplicationStorageService> logger)
{
    public async Task<CreditApplication?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient("creditapp-fileservice");

            var filesResponse = await httpClient.GetAsync("/api/files", cancellationToken);

            if (!filesResponse.IsSuccessStatusCode)
            {
                logger.LogWarning("Не удалось получить список файлов из FileService: {StatusCode}", filesResponse.StatusCode);
                return null;
            }

            var filesJson = await filesResponse.Content.ReadAsStringAsync(cancellationToken);
            var files = JsonSerializer.Deserialize<List<string>>(filesJson);

            var matchingFile = files?.FirstOrDefault(f => f.Contains($"credit-application-{id}-"));

            if (matchingFile == null)
            {
                logger.LogInformation("Файл для заявки {Id} не найден в MinIO", id);
                return null;
            }

            var fileResponse = await httpClient.GetAsync($"/api/files/{matchingFile}", cancellationToken);

            if (!fileResponse.IsSuccessStatusCode)
            {
                logger.LogWarning("Не удалось получить файл {FileName} из FileService", matchingFile);
                return null;
            }

            var fileContent = await fileResponse.Content.ReadAsStringAsync(cancellationToken);
            var application = JsonSerializer.Deserialize<CreditApplication>(fileContent);

            logger.LogInformation("Заявка {Id} успешно получена из MinIO", id);
            return application;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении заявки {Id} из хранилища", id);
            return null;
        }
    }
}
