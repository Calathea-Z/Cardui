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
    /// Returns when each payoff removes a minimum, and the breathing room that follows.
    /// The order is highest interest first and extra is zero, because those choices are not stored yet.
    /// </summary>
    [HttpGet("recovery")]
    [ProducesResponseType<CashFlowRecoveryReportDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CashFlowRecoveryReportDto>> GetRecovery(
        CancellationToken cancellationToken = default)
    {
        var report = await _planService.GetRecoveryAsync(cancellationToken);
        return Ok(report);
    }
}
