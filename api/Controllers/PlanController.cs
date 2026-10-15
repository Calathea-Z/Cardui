using Cardui.Api.Dtos.Plan;
using Cardui.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cardui.Api.Controllers;

[ApiController]
[Route("api/plan")]
public class PlanController : ControllerBase
{
    private readonly IPlanService _planService;
    private readonly IRestructuringService _restructuringService;

    public PlanController(IPlanService planService, IRestructuringService restructuringService)
    {
        _planService = planService;
        _restructuringService = restructuringService;
    }

    /// <summary>
    /// GET /api/plan/recovery
    /// Returns the payoff on rollover and on keeping every freed payment: balances over time, each minimum removed, the breathing room that follows,
    /// and the cash outlook for 30 days and 6, 12, and 18 months on each path.
    /// monthlyExtra is shared extra tried for this response. Omit it, or pass zero, for minimums only.
    /// incomeLossMonths leaves out included pay for that many months from today. Zero keeps every paycheck.
    /// windfallAmount and windfallOn add one cash inflow. livingSpendingMonthly and cashFloor replace those saved amounts when sent.
    /// A negative amount is rejected. Nothing is saved.
    /// </summary>
    [HttpGet("recovery")]
    [ProducesResponseType<PlanRecoveryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlanRecoveryDto>> GetRecovery(
        [FromQuery] PlanScenarioQuery scenario,
        CancellationToken cancellationToken = default)
    {
        var report = await _planService.GetRecoveryAsync(scenario, cancellationToken);
        return Ok(report);
    }

    /// <summary>
    /// POST /api/plan/restructuring
    /// Compares one consolidation loan, balance transfer, or replacement loan with selected debts.
    /// The result is not saved, and the offer is not treated as approved.
    /// </summary>
    [HttpPost("restructuring")]
    [ProducesResponseType<RestructuringComparisonDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RestructuringComparisonDto>> CompareRestructuring(
        [FromBody] CompareRestructuringDto dto,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _restructuringService.CompareAsync(dto, cancellationToken));
    }
}
