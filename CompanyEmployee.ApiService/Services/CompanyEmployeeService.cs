using System.Text.Json;
using CompanyEmployee.ApiService.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;

namespace CompanyEmployee.ApiService.Services;

/// <summary>
/// Сервис для получения данных сотрудника
/// </summary>
/// <param name="_cache">кэш Redis</param>
/// <param name="_generator">генератор сотрудника</param>
/// <param name="_logger">логгер</param>
public class CompanyEmployeeService(IDistributedCache _cache, CompanyEmployeeGenerator _generator, ILogger<CompanyEmployeeService> _logger, IConfiguration _configuration)
{
    private readonly int _cacheTime = _configuration.GetValue<int>("Constants:CacheTime");
    /// <summary>
    /// Получение данных сотрудника по его id, при отсутствии генерация сотрудника 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<CompanyEmployeeModel> GetEmployeeAsync(int id)
    {
        var cacheKey = $"employee:{id}";

        var cachedEmployee = await _cache.GetStringAsync(cacheKey);

        if (cachedEmployee != null)
        {
            var new_employee = JsonSerializer.Deserialize<CompanyEmployeeModel>(cachedEmployee);

            if (new_employee != null)
            {
                _logger.LogInformation("Сотрудник №{Id} получен из кэша", id);
                return new_employee;
            }
        }

        _logger.LogInformation("Генерация сотрудника №{Id}", id);

        var employee = _generator.Generate(id);

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(employee),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheTime)
            });

        return employee;
    }
}