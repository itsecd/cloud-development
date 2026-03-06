using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System;
using Service.Api.Entities;
using Microsoft.Extensions.Configuration;

namespace Service.Api.Generator;

public class GeneratorService(IDistributedCache _cache, IConfiguration _configuration, ILogger<GeneratorService> _logger) : IGeneratorService
{
    private readonly TimeSpan _cacheExpiration = int.TryParse(_configuration["CacheExpiration"], out var seconds)
        ? TimeSpan.FromSeconds(seconds)
        : TimeSpan.FromSeconds(3600);
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
            await PopulateCache(course);
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
        if(string.IsNullOrEmpty(json))
        {
            _logger.LogInformation("Course with ID: {CourseId} not found in cache", id);
            return null;
        }
        return JsonSerializer.Deserialize<StudyCourse>(json);
    }
    private async Task PopulateCache(StudyCourse course)
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