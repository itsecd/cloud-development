using System.ComponentModel.DataAnnotations;
using CourseGenerator.Api.Dto;
using CourseGenerator.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CourseGenerator.Api.Controllers;

[ApiController]
[Route("api/courses")]
public sealed class CourseContractsController(ICourseContractsService contractsService) : ControllerBase
{
    /// <summary>
    /// Генерирует список контрактов курсов с кэшированием результата в Redis.
    /// </summary>
    /// <param name="count">Количество контрактов для генерации (от 1 до 100).</param>
    /// <param name="cancellationToken">Токен отмены запроса.</param>
    /// <returns>Список сгенерированных контрактов курсов.</returns>
    /// <response code="200">Контракты успешно получены.</response>
    /// <response code="400">Передан недопустимый параметр count.</response>
    [HttpGet("generate")]
    [ProducesResponseType(typeof(IReadOnlyList<CourseContractDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<CourseContractDto>>> GenerateAsync(
        [FromQuery, Range(1, 100)] int count,
        CancellationToken cancellationToken)
    {
        try
        {
            var contracts = await contractsService.GenerateAsync(count, cancellationToken);
            var dto = contracts
                .Select(contract => new CourseContractDto(
                    contract.Id,
                    contract.CourseName,
                    contract.TeacherFullName,
                    contract.StartDate,
                    contract.EndDate,
                    contract.MaxStudents,
                    contract.CurrentStudents,
                    contract.HasCertificate,
                    contract.Price,
                    contract.Rating))
                .ToList();

            return Ok(dto);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            var problem = new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["count"] = [ex.Message]
            });
            return BadRequest(problem);
        }
    }
}
