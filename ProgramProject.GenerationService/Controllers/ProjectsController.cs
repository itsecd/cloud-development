using Microsoft.AspNetCore.Mvc;
using ProgramProject.GenerationService.Models;
using ProgramProject.GenerationService.Services;

namespace ProgramProject.GenerationService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProjectsController(IProjectService projectService, ILogger<ProjectsController> logger) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<ProgramProjectModel>> GetProject(int id)
    {
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

    [HttpGet]
    public async Task<ActionResult<ProgramProjectModel>> GetProjectByIdQuery(
    [FromQuery] int id)
    {
        return await GetProject(id);
    }
}