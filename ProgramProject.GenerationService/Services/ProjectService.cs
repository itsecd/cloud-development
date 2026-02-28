using Microsoft.Extensions.Caching.Distributed;
using ProgramProject.GenerationService.Generator;
using ProgramProject.GenerationService.Models;
using System.Text.Json;

namespace ProgramProject.GenerationService.Services;

/// <summary>
/// Сервис работы с кэшем и с объектами
/// </summary>
public class ProjectService : IProjectService
{
    private readonly IDistributedCache _cache;
    private readonly IProgramProjectFaker _faker;
    private readonly ILogger<ProjectService> _logger;
    private readonly DistributedCacheEntryOptions _cacheOptions;

    public ProjectService(
        IDistributedCache cache,
        IProgramProjectFaker faker,
        ILogger<ProjectService> logger,
        IConfiguration configuration)
    {
        _cache = cache;
        _faker = faker;
        _logger = logger;

        var cacheMinutes = configuration.GetValue("Cache:ExpirationMinutes", 5);
        _cacheOptions = new DistributedCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(cacheMinutes));
    }

    public async Task<ProgramProjectModel> GetProjectByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"project:{id}";

        try
        {
            // Получаем из кэша
            var cachedBytes = await _cache.GetAsync(cacheKey, cancellationToken);

            if (cachedBytes != null)
            {
                var cachedProject = JsonSerializer.Deserialize<ProgramProjectModel>(cachedBytes);

                if (cachedProject != null)
                {
                    _logger.LogInformation("Проект с ID {ProjectId} найден в кэше", id);
                    return cachedProject;
                }

                _logger.LogWarning("Проект с ID {ProjectId} найден в кэше, но повреждён. Удаляем...", id);
                await _cache.RemoveAsync(cacheKey);
            }

            _logger.LogInformation("Проект с ID {ProjectId} не найден в кэше. Генерируем новый", id);

            // Генерируем новый проект
            var newProject = _faker.Generate();
            newProject.Id = id;

            // СОхраняем в кэш
            var serializedProject = JsonSerializer.SerializeToUtf8Bytes(newProject);
            await _cache.SetAsync(cacheKey, serializedProject, _cacheOptions, cancellationToken);

            _logger.LogInformation("Проект с ID {ProjectId} сгенерирован и сохранён в кэш", id);

            return newProject;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении проекта с ID {ProjectId}", id);
            throw;
        }
    }
}