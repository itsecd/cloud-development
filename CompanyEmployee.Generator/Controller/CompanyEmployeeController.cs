using CompanyEmployee.Generator.Dto;
using CompanyEmployee.Generator.Service;
using Microsoft.AspNetCore.Mvc;

namespace CompanyEmployee.Generator.Controller;

/// <summary>
/// Контроллер для получения сотрудника компании по id
/// </summary>
[ApiController]
[Route("company-employee")]
public class CompanyEmployeeController(
    CompanyEmployeeService service,
    Logger<CompanyEmployeeController> logger
    ) : ControllerBase
{
    /// <summary>
    /// Метод для получения сотрудника компании по id 
    /// </summary>
    /// <param name="id">Идентификатор сотрудника</param>
    /// <param name="cancellationToken">Токен отмены запроса</param>
    /// <returns>DTO сотрудника компании</returns>
    /// <response code="200">Успешное получение сотрудника</response>
    /// <response code="400">Некорректный id</response>
    [HttpGet]
    public async Task<ActionResult<CompanyEmployeeDto>> GetById([FromQuery] int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest("Id must be greater or equal than 0");
        }
        logger.LogInformation($"HTTP GET /company-employee, id: {id}", id);
        
        var employee = await service.GetByIdAsync(id, cancellationToken);
        return Ok(employee);
    }
}