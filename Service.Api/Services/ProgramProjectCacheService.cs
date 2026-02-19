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
public class ProgramProjectCacheService(RedisCacheService cs, Faker<ProgramProject> faker)
{
    private RedisCacheService _cs = cs;
    private Faker<ProgramProject> _faker = faker;
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
            var programProject = await _cs.GetAsync<ProgramProject>(key);
            if (programProject != null) return programProject;
        }
        catch (RedisException ex) 
        {
            Console.WriteLine(ex.Message);
        }
        ProgramProject newProject = _faker.Generate();
        await _cs.SetAsync(key, newProject, TimeSpan.FromHours(12));
        return newProject with { Id = id };
    }
}
