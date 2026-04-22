using Microsoft.Extensions.Caching.Distributed;
using Service.Api.Entities;
using System.Text.Json;
namespace Service.Api.Generator;

public class EmployeeGeneratorService(IDistributedCache cache, ILogger<EmployeeGeneratorService> logger, IConfiguration configuration) : IEmployeeGeneratorService
{
    /// <summary>
    /// Время инициализации кэша
    /// </summary>
    private readonly TimeSpan _cacheExpiration = int.TryParse(configuration["CacheExpiration"], out var sec)
        ? TimeSpan.FromSeconds(sec)
        : TimeSpan.FromSeconds(3600);

    public async Task<Employee> ProcessEmployee(int id)
    {
        logger.LogInformation("Обработка сотрудника {id} ", id);
        try
        {
            logger.LogInformation("Trying to get employee {id} from cache", id);
            var employee = await RetrieveFromCache(id);
            if (employee != null)
            {
                logger.LogInformation("Employee {id} was found in cache", id);
                return employee;
            }
            logger.LogInformation("No employee {id} in cache. Generating employee", id);
            employee = EmployeeGenerator.Generate(id);
            logger.LogInformation("Populating the cache with employee {id}", id);
            await PopulateCache(employee);
            return employee;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred during employee {id} processing", id);
            throw;
        }

    }

    /// <summary>
    /// Пытается достать сотрудника из кэша
    /// </summary>
    private async Task<Employee?> RetrieveFromCache(int id)
    {
        var json = await cache.GetStringAsync(id.ToString());
        if (string.IsNullOrEmpty(json))
            return null;
        return JsonSerializer.Deserialize<Employee>(json);
    }

    /// <summary>
    /// Кладет сотрудника в кэш
    /// </summary>
    private async Task PopulateCache(Employee employee)
    {
        var json = JsonSerializer.Serialize(employee);
        await cache.SetStringAsync(employee.Id.ToString(), json, 
            new DistributedCacheEntryOptions 
            { 
                AbsoluteExpirationRelativeToNow = _cacheExpiration 
            });
    }

}
