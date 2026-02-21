using CourseManagement.ApiService.Models;
using CourseManagement.ApiService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace CourseManagement.ApiService.Controllers;

[ApiController]
[Route("course-management")]
public class CourseController(CourseGenerator generator, IDistributedCache cache, ILogger<CourseController> logger, IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Course>> GetCourse(int? id)
    {
        try
        {
            logger.LogInformation("Request received with id: {Id}", id);

            var cacheKey = $"course:{id ?? 0}";
            var cachedCourse = await cache.GetStringAsync(cacheKey);

            if (cachedCourse != null)
            {
                logger.LogInformation("Course found in cache for id: {Id}", id);
                var course = JsonSerializer.Deserialize<Course>(cachedCourse);
                return Ok(course);
            }

            logger.LogInformation("Course not in cache, generating new for id: {Id}", id);
            var newCourse = generator.GenerateOne(id);

            var cacheDuration = configuration.GetValue<double?>("Cache:DurationMinutes") ?? 5;
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheDuration)
            };

            var serializedCourse = JsonSerializer.Serialize(newCourse);
            await cache.SetStringAsync(cacheKey, serializedCourse, options);

            logger.LogInformation("The course was successfully generated for id: {Id}", id);
            return Ok(newCourse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during course generation");
            return Problem("Internal server error", statusCode: 500);
        }
    }
}
