using System.Text.Json;
using CompanyEmployee.Generator.Dto;
using CompanyEmployee.Generator.Messaging;
using Microsoft.Extensions.Caching.Distributed;

namespace CompanyEmployee.Generator.Service;

/// <summary>
/// Сервис получения сотрудника компании
/// </summary>
/// <param name="generator">Генератор сотрудника по идентификатору</param>
/// <param name="producerService">Служба отправки сообщений в брокер</param>
/// <param name="cache">Сервис кэширования</param>
/// <param name="configuration">Конфигурация приложения</param>
/// <param name="logger">Логгер</param>
public class CompanyEmployeeService(
    ICompanyEmployeeGenerator generator,
    IProducerService producerService,
    IDistributedCache cache,
    IConfiguration configuration,
    ILogger<CompanyEmployeeService> logger
    ) : ICompanyEmployeeService
{
    private static readonly string _companyEmployeeCachePrefix = "company-employee:"; 
    
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    
    /// <inheritdoc/>
    public async Task<CompanyEmployeeDto> GetByIdAsync(int employeeId, CancellationToken token)
    {
        var cacheKey = _companyEmployeeCachePrefix + employeeId;
        var cachedValue = await cache.GetStringAsync(cacheKey, token);

        CompanyEmployeeDto companyEmployee;

        if (!string.IsNullOrEmpty(cachedValue))
        {
            logger.LogInformation("Read from cache, key: {cacheKey}", cacheKey);
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
                logger.LogWarning(ex, "Error deserializing cached employee, key: {cacheKey}", cacheKey);
            }
        }

        companyEmployee = generator.Generate(employeeId);

        await producerService.SendMessage(companyEmployee);
        
        var ttlSeconds = configuration.GetValue("CompanyEmployeeCache:TtlSeconds", 600);
        var cacheOpts = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttlSeconds)
        };
        
        try
        {
            await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(companyEmployee, _jsonOptions), cacheOpts,
                token);
            logger.LogInformation("Write to cache, key: {cacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error in caching companyEmployee, key: {cacheKey}", cacheKey);
        }
        
        return companyEmployee;
    }
}