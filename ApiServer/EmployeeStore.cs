using System.Collections.Concurrent;
using Contracts;

namespace ApiServer;

public class EmployeeStore
{
    private readonly ConcurrentDictionary<int, Employee> _employees = new();
    private readonly CacheService _cacheService;
    private readonly ILogger<EmployeeStore> _logger;

    public EmployeeStore(CacheService cacheService, ILogger<EmployeeStore> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task AddEmployeesAsync(List<Employee> employees)
    {
        foreach (var emp in employees)
        {
            _employees[emp.Id] = emp;
            await _cacheService.SetAsync($"employee:{emp.Id}", emp);
        }

        // Инвалидируем кэш списка
        await _cacheService.RemoveAsync("employees:all");
        _logger.LogInformation("Added {Count} employees to store", employees.Count);
    }

    public async Task<Employee?> GetByIdAsync(int id)
    {
        // Сначала проверяем кэш
        var cached = await _cacheService.GetAsync<Employee>($"employee:{id}");
        if (cached is not null)
            return cached;

        // Если нет в кэше, берём из памяти
        if (_employees.TryGetValue(id, out var employee))
        {
            await _cacheService.SetAsync($"employee:{id}", employee);
            return employee;
        }

        return null;
    }

    public async Task<List<Employee>> GetAllAsync()
    {
        var cached = await _cacheService.GetAsync<List<Employee>>("employees:all");
        if (cached is not null)
            return cached;

        var all = _employees.Values.ToList();
        await _cacheService.SetAsync("employees:all", all);
        return all;
    }
}
