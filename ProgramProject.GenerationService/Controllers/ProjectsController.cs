using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using ProgramProject.GenerationService.Generator;
using ProgramProject.GenerationService.Models;
using System.Text.Json;

namespace ProgramProject.GenerationService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProjectsController : ControllerBase
{
    private readonly IDistributedCache _cache;
    private readonly ProgramProjectFaker _faker;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(
        IDistributedCache cache,
        ProgramProjectFaker faker,
        ILogger<ProjectsController> logger)
    {
        _cache = cache;
        _faker = faker;
        _logger = logger;
    }


    [HttpGet]
    public async Task<ActionResult<ProgramProjectModel>> GetProjects(
        [FromQuery] int? id,
        [FromQuery] int count = 5)
    {
        // Если передан id - возвращаем ОДИН объект
        if (id.HasValue)
        {
            var cacheKey = $"project:{id.Value}";

            _logger.LogInformation("Запрос проекта с ID {ProjectId} через query. Проверка кэша...", id);

            var cachedBytes = await _cache.GetAsync(cacheKey);

            if (cachedBytes != null)
            {
                var cachedProject = JsonSerializer.Deserialize<ProgramProjectModel>(cachedBytes);
                _logger.LogInformation("Проект с ID {ProjectId} найден в кэше", id);
                return Ok(cachedProject);
            }

            _logger.LogInformation("Проект с ID {ProjectId} не найден в кэше. Генерируем новый", id);

            var newProject = _faker.Generate();
            newProject.Id = id.Value;

            var options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

            var serializedProject = JsonSerializer.SerializeToUtf8Bytes(newProject);
            await _cache.SetAsync(cacheKey, serializedProject, options);

            _logger.LogInformation("Проект с ID {ProjectId} сгенерирован и сохранён в кэш", id);

            return Ok(newProject);
        }

        // Если id не передан - возвращаем список проектов
        if (count < 1 || count > 20)
        {
            return BadRequest("Количество проектов должно быть от 1 до 20");
        }

        var projects = _faker.Generate(count);
        return Ok(projects);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProgramProjectModel>> GetProject(int id)
    {
        return await GetProjects(id);
    }
}