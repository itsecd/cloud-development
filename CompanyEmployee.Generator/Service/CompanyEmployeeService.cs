using System.Text.Json;
using CompanyEmployee.Generator.Dto;
using Microsoft.Extensions.Caching.Distributed;

namespace CompanyEmployee.Generator.Service;

/// <summary>
/// Сервис получения сотрудника компании
/// </summary>
public class CompanyEmployeeService(
    CompanyEmployeeGenerator generator,
    IDistributedCache cache,
    IConfiguration configuration,
    ILogger<CompanyEmployeeService> logger
    )
{
    private static readonly string _companyEmployeeCachePrefix = "company-employee:"; 
    
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    
    /// <summary>
    /// Метод получения сотрудника компании по идентификатору
    /// Сначала пытается найти сотрудника в кэше, если не находит, то генерирует нового и записывает его в кэш
    /// </summary>
    /// <param name="id">Идентификатор сотрудника</param>
    /// <param name="token">Токен отмены запроса</param>
    /// <returns>DTO сотрудника компании</returns>
    public async Task<CompanyEmployeeDto> GetByIdAsync(int id, CancellationToken token)
    {
        var cacheKey = _companyEmployeeCachePrefix + id;
        var cachedValue = await cache.GetStringAsync(cacheKey, token);

        CompanyEmployeeDto companyEmployee;

        if (!string.IsNullOrEmpty(cachedValue))
        {
            logger.LogInformation("Read from cache, key: {}", cacheKey);
            try
            {
                companyEmployee = JsonSerializer.Deserialize<CompanyEmployeeDto>(cachedValue, _jsonOptions);
                if (companyEmployee != null)
                {
                    return companyEmployee;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error deserializing cached employee, key: {}", cacheKey);
            }
        }

        companyEmployee = generator.Generate(id);
        
        var ttlSeconds = configuration.GetValue("CompanyEmployeeCache:TtlSeconds", 600);
        var cacheOpts = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttlSeconds)
        };
        
        try
        {
            await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(companyEmployee, _jsonOptions), cacheOpts,
                token);
            logger.LogInformation("Write to cache, key: {}", cacheKey);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error in caching companyEmployee, key: {}", cacheKey);
        }
        
        return companyEmployee;
    }
}