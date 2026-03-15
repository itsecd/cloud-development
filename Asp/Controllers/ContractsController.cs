using Bogus;
using Domain.Contracts;
using Domain.Interfaces;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("contracts")]
public class ContractsController : ControllerBase
{
    private readonly IVehicleContractCachedService _service;
    private ILogger<ContractsController> _logger;


    public ContractsController(IVehicleContractCachedService service, ILogger<ContractsController> logger)
    {
        _service = service;
        _logger = logger;
    }


    [HttpGet("vehicle")]
    public async Task<ActionResult<VehicleContractDto>> GenerateVehicle([FromQuery] int? seed = null)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var actualSeed = seed ?? Random.Shared.Next();


       _logger.LogInformation(
       "Starting generation of vehicle contract. Ip: {IpAddress}, SeedFromQuery: {SeedFromQuery}, ActualSeed: {ActualSeed}",
       ip,
       seed,
       actualSeed);

        var contract = await _service.GetVehicleContractAsync(actualSeed);
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
