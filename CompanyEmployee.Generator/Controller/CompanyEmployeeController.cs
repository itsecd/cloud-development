using CompanyEmployee.Generator.Dto;
using CompanyEmployee.Generator.Service;
using Microsoft.AspNetCore.Mvc;

namespace CompanyEmployee.Generator.Controller;

[ApiController]
[Route("company-employee")]
public class CompanyEmployeeController(
    CompanyEmployeeService service
    ) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CompanyEmployeeDto>> GetById([FromQuery] int id, CancellationToken cancellationToken)
    {
        var employee = await service.GetByIdAsync(id, cancellationToken);
        return Ok(employee);
    }
}