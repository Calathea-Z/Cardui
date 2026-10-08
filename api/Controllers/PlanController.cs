using Cardui.Api.Dtos.Plan;
using Cardui.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cardui.Api.Controllers;

[ApiController]
[Route("api/plan")]
public class PlanController : ControllerBase
{
    private readonly IPlanService _planService;

    public PlanController(IPlanService planService)
    {
        _planService = planService;
    }

    /// <summary>
    /// GET /api/plan/recovery
    /// Returns the payoff on rollover and on keeping every freed payment: balances over time, each minimum removed, and the breathing room that follows.
    /// The order is highest interest first and extra is zero, because those choices are not stored yet.
    /// </summary>
    [HttpGet("recovery")]
    [ProducesResponseType<PlanRecoveryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PlanRecoveryDto>> GetRecovery(
        CancellationToken cancellationToken = default)
    {
        var report = await _planService.GetRecoveryAsync(cancellationToken);
        return Ok(report);
    }
}
