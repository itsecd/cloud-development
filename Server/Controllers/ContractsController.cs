using Microsoft.AspNetCore.Mvc;

using Domain.Interfaces;
using Domain.Contracts;
using Infrastructure.Generators;
using Domain;

[ApiController]
[Route("contracts")]
public class ContractsController : ControllerBase
{
    private readonly IVehicleContractGenerator _generator;

    public ContractsController(IVehicleContractGenerator service)
    {
        _generator = service;
    }

    [HttpGet("vehicle")]
    public ActionResult<IVehicleContractGenerator> GenerateVehicle([FromQuery] int? seed = null)
    {
        var contract = _generator.Generate(seed);
        VehicleContractValidator.Validate(contract);
        return Ok(contract);
    }
}