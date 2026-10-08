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
    /// Returns the payoff on rollover and on keeping every freed payment: balances over time, each minimum removed, the breathing room that follows,
    /// and the cash outlook for 30 days and 6, 12, and 18 months on each path.
    /// monthlyExtra is shared extra tried for this response. Omit it, or pass zero, for minimums only. A negative amount is rejected. Nothing is saved.
    /// </summary>
    [HttpGet("recovery")]
    [ProducesResponseType<PlanRecoveryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlanRecoveryDto>> GetRecovery(
        [FromQuery] decimal monthlyExtra = 0,
        CancellationToken cancellationToken = default)
    {
        var report = await _planService.GetRecoveryAsync(monthlyExtra, cancellationToken);
        return Ok(report);
    }
}
