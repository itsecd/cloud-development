using Domain.Contracts;
using Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Контроллер для генерации контрактов.
/// </summary>
[ApiController]
[Route("contracts")]
public class ContractsController(IVehicleContractCachedService service,
    ILogger<ContractsController> logger) : ControllerBase
{
    /// <summary>
    /// Получить сгенерированный контракт транспортного средства.
    /// </summary>
    /// <param name="Id">Id контракта.</param>
    /// <returns>Сгенерированный контракт.</returns>
    [HttpGet("vehicle")]
    public async Task<ActionResult<VehicleContractDto>> GenerateVehicle([FromQuery] int? id = null)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var actualid = id ?? Random.Shared.Next();

        logger.LogInformation(
            "Starting generation of vehicle contract. Ip: {IpAddress}," +
            " idFromQuery: {idFromQuery}, Actualid: {Actualid}", ip, id, actualid);

        var contract = await service.GetVehicleContractAsync(actualid);
        VehicleContractValidator.Validate(contract);
        logger.LogInformation(
        "Vehicle contract generated successfully. Ip: {IpAddress}, Manufacturer: {Manufacturer}, " +
        "Model: {Model}, Year: {Year}", ip, contract.Manufacturer, contract.Model, contract.Year);

        return Ok(contract);
    }
}