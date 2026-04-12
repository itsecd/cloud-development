using Microsoft.AspNetCore.Mvc;
using CourseManagement.ApiService.Entities;
using CourseManagement.ApiService.Services;

namespace CourseManagement.ApiService.Controllers;

/// <summary>
/// Контроллер для сущности типа Курс
/// </summary>
/// <param name="logger">Логгер</param>
/// <param name="courseService">Сервис для сущности типа Курс</param>
[ApiController]
[Route("course-management")]
public class CourseController(ILogger<CourseController> logger, ICourseService courseService) : ControllerBase
{
    /// <summary>
    /// Обработчик GET-запроса на генерацию курса
    /// </summary>
    /// <param name="id">Идентификатор курса</param>
    /// <returns>Сгенерированный курс</returns>
    [HttpGet]
    public async Task<ActionResult<Course>> GetCourse(int? id)
    {
        logger.LogInformation("Processing request for course {ResourceId}", id);

        var course = await courseService.GetCourse(id ?? 0);

        return Ok(course);
    }
}