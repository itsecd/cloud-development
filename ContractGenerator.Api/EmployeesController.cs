using ContractGenerator.Api.Services;
using ContractGenerator.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace ContractGenerator.Api;

/// <summary>
/// Контроллер для получения сотрудника компании по id.
/// </summary>
/// <param name="employeeService">Сервис получения сотрудника компании.</param>
/// <param name="logger">Логгер.</param>
[ApiController]
[Route("employee")]
public class EmployeesController(
    IEmployeeService employeeService,
    ILogger<EmployeesController> logger) : ControllerBase
{
    /// <summary>
    /// Получает сотрудника компании по id.
    /// </summary>
    /// <param name="id">Идентификатор сотрудника.</param>
    /// <returns>Информация о сотруднике компании.</returns>
    /// <response code="200">Успешное получение сотрудника компании.</response>
    /// <response code="400">Некорректный id сотрудника.</response>
    [ProducesResponseType(typeof(Employee), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [HttpGet]
    public async Task<ActionResult<Employee>> GetEmployee([FromQuery] int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { error = "Id must be a positive number" });
        }

        logger.LogInformation("HTTP GET /employee, id: {EmployeeId}", id);

        var employee = await employeeService.GetOrGenerateAsync(id);
        return Ok(employee);
    }
}
