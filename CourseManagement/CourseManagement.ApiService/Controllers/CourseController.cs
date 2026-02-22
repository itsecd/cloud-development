using Microsoft.AspNetCore.Mvc;
using CourseManagement.ApiService.Dto;
using CourseManagement.ApiService.Services;

namespace CourseManagement.ApiService.Controllers;

/// <summary>
/// Контроллер для сущности типа Курс
/// </summary>
/// <param name="logger">Логгер</param>
/// <param name="courseService">Сервис для сущности типа Курс</param>
[ApiController]
[Route("course-management")]
public class CourseController(ILogger<CourseController> logger, CourseService courseService) : ControllerBase
{
    /// <summary>
    /// Обработчик GET-запроса на генерацию курса
    /// </summary>
    /// <param name="id">Идентификатор курса</param>
    /// <returns>Сгенерированный курс</returns>
    [HttpGet]
    public async Task<ActionResult<CourseDto>> GetCourse(int? id)
    {
        using (logger.BeginScope(new
        {
            RequestId = Guid.NewGuid(),
            ResourceType = "Course",
            ResourceId = id,
            Operation = "GetCourse"
        }))
        {
            var course = await courseService.GetCourse(id ?? 0);

            return course != null ? Ok(course) : Problem("Internal server error", statusCode: 500);
        }
    }
}