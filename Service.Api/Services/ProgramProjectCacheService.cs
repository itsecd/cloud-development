using Bogus;
using Service.Api.Entity;
using Service.Api.Generator;
using Service.Api.Redis;
using StackExchange.Redis;

namespace Service.Api.Services;

/// <summary>
/// Provides cached access to <see cref="ProgramProject"/> instances.
/// Generates a new project if it is not found in Redis.
/// </summary>
public class ProgramProjectCacheService(RedisService cacheService, Faker<ProgramProject> faker)
{
    /// <summary>
    /// Returns a cached <see cref="ProgramProject"/> by ID,
    /// or generates and stores a new one  if not found.
    /// </summary>
    /// <param name="id">Id of the project.</param>
    /// <returns>The existing or newly generated <see cref="ProgramProject"/>.</returns>
    public async Task<ProgramProject> GetOrGenerateAsync(int id)
    {
        var key = $"project:{id}";
        try
        {
            var programProject = await cacheService.GetAsync<ProgramProject>(key);
            if (programProject != null) return programProject;
        }
        catch (RedisException ex) 
        {
            Console.WriteLine(ex.Message);
        }
        var newProject = faker.Generate() with { Id = id };
        try
        {
            await cacheService.SetAsync(key, newProject, TimeSpan.FromHours(12));
        }
        catch (RedisException ex) 
        {
            Console.WriteLine(ex.Message);
        }
        return newProject;
    }
}
