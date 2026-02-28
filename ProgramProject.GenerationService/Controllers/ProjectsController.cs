using Microsoft.AspNetCore.Mvc;
using ProgramProject.GenerationService.Models;
using ProgramProject.GenerationService.Services;

namespace ProgramProject.GenerationService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProjectsController(IProjectService projectService, ILogger<ProjectsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ProgramProjectModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProgramProjectModel>> GetProject([FromQuery] int id)
    {
        if (id <= 0)
        {
            return BadRequest("ID должен быть положительным числом");
        }

        try
        {
            logger.LogInformation("Запрос проекта с ID {ProjectId}", id);
            var project = await projectService.GetProjectByIdAsync(id);
            return Ok(project);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обработке запроса проекта ID {ProjectId}", id);
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }
}