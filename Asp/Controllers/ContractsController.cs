using Domain.Contracts;
using Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("contracts")]
public class ContractsController(IVehicleContractCachedService service, ILogger<ContractsController> logger) : ControllerBase
{
    private readonly IVehicleContractCachedService _service = service;
    private ILogger<ContractsController> _logger = logger;


    [HttpGet("vehicle")]
    public async Task<ActionResult<VehicleContractDto>> GenerateVehicle([FromQuery] int? id = null)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var actualid = id ?? Random.Shared.Next();

       _logger.LogInformation(
       "Starting generation of vehicle contract. Ip: {IpAddress}, idFromQuery: {idFromQuery}, Actualid: {Actualid}",
       ip,
       id,
       actualid);

        var contract = await _service.GetVehicleContractAsync(actualid);
        VehicleContractValidator.Validate(contract);
        _logger.LogInformation(
        "Vehicle contract generated successfully. Ip: {IpAddress}, Manufacturer: {Manufacturer}, Model: {Model}, Year: {Year}",
        ip,
        contract.Manufacturer,
        contract.Model,
        contract.Year);

        return Ok(contract);
    }
}
