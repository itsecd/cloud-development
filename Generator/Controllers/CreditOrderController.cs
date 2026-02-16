using Generator.Dto;
using Generator.Services;
using Microsoft.AspNetCore.Mvc;

namespace Generator.Controllers;


[ApiController]
[Route("credit-orders")]
public sealed class CreditOrderController : ControllerBase
{
    private readonly ILogger<CreditOrderController> _logger;
    private readonly CreditOrderService _service;

    public CreditOrderController(CreditOrderService service, ILogger<CreditOrderController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<CreditOrderDto>> Get([FromQuery] int id, CancellationToken ct)
    {
        _logger.LogInformation("HTTP GET /credit-orders requested: {OrderId}", id);

        var order = await _service.GetByIdAsync(id, ct);

        _logger.LogInformation("HTTP GET /credit-orders completed: {OrderId} {Status}", order.Id, order.OrderStatus);

        return Ok(order);
    }
}
