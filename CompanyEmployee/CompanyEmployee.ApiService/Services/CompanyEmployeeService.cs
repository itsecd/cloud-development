using System.Text.Json;
using CompanyEmployee.ApiService.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace CompanyEmployee.ApiService.Services;

/// <summary>
/// Сервис для получения данных сотрудника
/// </summary>
/// <param name="_cache">кэш Redis</param>
/// <param name="_generator">генератор сотрудника</param>
/// <param name="_logger">логгер</param>
public class CompanyEmployeeService(IDistributedCache _cache, CompanyEmployeeGenerator _generator, ILogger<CompanyEmployeeService> _logger)
{
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
            _logger.LogInformation("Сотрудник №{Id} получен из кэша", id);

            return JsonSerializer.Deserialize<CompanyEmployeeModel>(cachedEmployee)!;
        }

        _logger.LogInformation("Генерация сотрудника №{Id}", id);

        var employee = _generator.Generate(id);

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(employee),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

        return employee;
    }
}