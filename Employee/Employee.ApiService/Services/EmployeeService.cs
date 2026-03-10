using Employee.ApiService.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Employee.ApiService.Services;

/// <summary>
/// Сервис получения сотрудников
/// </summary>
/// <param name="_cache">кэш</param>
/// <param name="_configuration">конфигурация</param>
/// <param name="_logger">логирование</param>
/// <param name="_generator">генератор</param>
public class EmployeeService(
    IDistributedCache _cache,
    IConfiguration _configuration,
    ILogger<EmployeeService> _logger,
    EmployeeGenerator _generator)
{

    /// <summary>
    /// Получение сотрудника по id
    /// </summary>
    /// <param name="id">идентификатор</param>
    /// <returns></returns>
    public async Task<EmployeeModel> GetEmployeeAsync(int id)
    {
        var cacheKey = $"employee:{id}";

        _logger.LogInformation("Попытка получить сотрудника {EmployeeId} из кэша", id);

        var cachedData = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedData))
        {
            try
            {
                var cachedEmployee = JsonSerializer.Deserialize<EmployeeModel>(cachedData);

                if (cachedEmployee != null)
                {
                    _logger.LogInformation("Сотрудник {EmployeeId} получен из кэша", id);
                    return cachedEmployee;
                }

                _logger.LogWarning("Сотрудник {EmployeeId} найден в кэше, но десериализация вернула null", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка десериализации сотрудника {EmployeeId}", id);
            }
        }

        _logger.LogInformation("Сотрудник {EmployeeId} отсутствует в кэше. Генерация нового", id);

        var employee = _generator.Generate(id);

        try
        {
            var expirationMinutes = _configuration.GetValue("CacheSettings:ExpirationMinutes", 5);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expirationMinutes)
            };

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(employee),
                cacheOptions
            );

            _logger.LogInformation("Сотрудник {EmployeeId} сохранён в кэш", id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось сохранить сотрудника {EmployeeId} в кэш", id);
        }

        return employee;
    }

}
