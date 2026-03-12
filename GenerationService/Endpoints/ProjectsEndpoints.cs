using System.Text.Json;
using GenerationService.Models;
using GenerationService.Services;
using Microsoft.Extensions.Caching.Distributed;

namespace GenerationService.Endpoints;

public static class ProjectsEndpoints
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public static void MapProjectsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/projects")
            .WithTags("Projects");

        group.MapGet("/generate", GenerateSingle)
            .WithName("GenerateSingleProject")
            .WithSummary("Генерация одного программного проекта (с кэшированием)");

        group.MapGet("/generate/{count:int}", GenerateMany)
            .WithName("GenerateManyProjects")
            .WithSummary("Генерация нескольких программных проектов (с кэшированием)");

        group.MapDelete("/cache", ClearCache)
            .WithName("ClearCache")
            .WithSummary("Очистка кэша для обновления данных");
    }

    private static async Task<IResult> GenerateSingle(
        IDistributedCache cache,
        ProjectGeneratorService generator,
        ILogger<ProjectGeneratorService> logger)
    {
        const string cacheKey = "project:single";

        var cached = await cache.GetStringAsync(cacheKey);
        if (cached is not null)
        {
            logger.LogInformation("Cache HIT for key: {CacheKey}", cacheKey);
            var cachedProject = JsonSerializer.Deserialize<SoftwareProject>(cached);
            return Results.Ok(cachedProject);
        }

        logger.LogInformation("Cache MISS for key: {CacheKey}. Generating new project...", cacheKey);
        var project = generator.Generate();
        var json = JsonSerializer.Serialize(project);
        await cache.SetStringAsync(cacheKey, json, CacheOptions);

        logger.LogInformation("Generated project: {ProjectName} [{Language}]", project.Name, project.ProgrammingLanguage);
        return Results.Ok(project);
    }

    private static async Task<IResult> GenerateMany(
        int count,
        IDistributedCache cache,
        ProjectGeneratorService generator,
        ILogger<ProjectGeneratorService> logger)
    {
        if (count is <= 0 or > 100)
            return Results.BadRequest("Count must be between 1 and 100.");

        var cacheKey = $"projects:list:{count}";

        var cached = await cache.GetStringAsync(cacheKey);
        if (cached is not null)
        {
            logger.LogInformation("Cache HIT for key: {CacheKey}", cacheKey);
            var cachedProjects = JsonSerializer.Deserialize<List<SoftwareProject>>(cached);
            return Results.Ok(cachedProjects);
        }

        logger.LogInformation("Cache MISS for key: {CacheKey}. Generating {Count} projects...", cacheKey, count);
        var projects = generator.Generate(count);
        var json = JsonSerializer.Serialize(projects);
        await cache.SetStringAsync(cacheKey, json, CacheOptions);

        logger.LogInformation("Generated {Count} projects successfully", projects.Count);
        return Results.Ok(projects);
    }

    private static async Task<IResult> ClearCache(
        IDistributedCache cache,
        ILogger<ProjectGeneratorService> logger)
    {
        await cache.RemoveAsync("project:single");
        for (var i = 1; i <= 100; i++)
            await cache.RemoveAsync($"projects:list:{i}");

        logger.LogInformation("Cache cleared successfully");
        return Results.Ok(new { message = "Cache cleared successfully" });
    }
}
