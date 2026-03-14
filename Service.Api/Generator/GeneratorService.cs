using Microsoft.Extensions.Caching.Distributed;
using Service.Api.Entities;
using System.Text.Json;

namespace Service.Api.Generator;

public class GeneratorService(IDistributedCache _cache, IConfiguration _configuration, ILogger<GeneratorService> _logger) : IGeneratorService
{
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromSeconds(_configuration.GetSection("Cache").GetValue("CacheExpiration", 3600));
    public async Task<StudyCourse> ProcessCourse(int id)
    {
        _logger.LogInformation("Processing course with ID: {CourseId}", id);
        try
        {
            _logger.LogInformation("Attempting to retrieve course from cache with ID: {CourseId}", id);
            var course = await RetrieveFromCache(id);
            if (course != null)
            {
                _logger.LogInformation("Course with ID: {CourseId} retrieved from cache", id);
                return course;
            }
            _logger.LogInformation("Course with ID: {CourseId} not found in cache, generating new course", id);
            course = StudyCoureGenerator.GenerateCourse(id);
            _logger.LogInformation("Populating cache with course {id}", id);
            await SetCache(course);
            return course;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing course with ID: {id}", id);
            throw;
        }
    }
    private async Task<StudyCourse> RetrieveFromCache(int id)
    {
        var json = await _cache.GetStringAsync(id.ToString());
        if (!string.IsNullOrEmpty(json))
        {
            return JsonSerializer.Deserialize<StudyCourse>(json);
        }
        _logger.LogInformation("Course with ID: {CourseId} not found in cache", id);
        return null;
    }
    private async Task SetCache(StudyCourse course)
    {
        var json = JsonSerializer.Serialize(course);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration
        };
        await _cache.SetStringAsync(course.Id.ToString(), json, options);
        _logger.LogInformation("Course with ID: {CourseId} stored in cache with expiration of {Expiration}", course.Id, _cacheExpiration);
    }
}