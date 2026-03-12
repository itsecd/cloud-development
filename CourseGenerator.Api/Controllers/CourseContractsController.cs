using System.ComponentModel.DataAnnotations;
using CourseGenerator.Api.Dto;
using CourseGenerator.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CourseGenerator.Api.Controllers;

[ApiController]
[Route("api/courses")]
public sealed class CourseContractsController(
    ICourseContractsService contractsService,
    ICourseContractGenerator contractGenerator) : ControllerBase
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

    /// <summary>
    /// Возвращает один сгенерированный контракт по идентификатору для совместимости с клиентом.
    /// </summary>
    /// <param name="id">Неотрицательный идентификатор объекта.</param>
    /// <param name="cancellationToken">Токен отмены запроса.</param>
    /// <returns>Сгенерированный контракт.</returns>
    /// <response code="200">Контракт успешно получен.</response>
    /// <response code="400">Передан недопустимый параметр id.</response>
    [HttpGet("by-id")]
    [ProducesResponseType(typeof(CourseContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<CourseContractDto> GetByIdAsync(
        [FromQuery, Range(0, int.MaxValue)] int id,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contract = contractGenerator.GenerateById(id);

        return Ok(new CourseContractDto(
            contract.Id,
            contract.CourseName,
            contract.TeacherFullName,
            contract.StartDate,
            contract.EndDate,
            contract.MaxStudents,
            contract.CurrentStudents,
            contract.HasCertificate,
            contract.Price,
            contract.Rating));
    }
}
