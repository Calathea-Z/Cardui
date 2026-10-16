using Cardui.Api.Domain.Recovery;
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
    private readonly ISavedPlanService _savedPlanService;

    public PlanController(
        IPlanService planService,
        IRestructuringService restructuringService,
        ISavedPlanService savedPlanService)
    {
        _planService = planService;
        _restructuringService = restructuringService;
        _savedPlanService = savedPlanService;
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

    /// <summary>
    /// GET /api/plan/decision
    /// Returns the plain-language decision for the current facts and the selected scenario.
    /// Nothing is saved. kind is Rollover or ReclaimAll.
    /// </summary>
    [HttpGet("decision")]
    [ProducesResponseType<DecisionPlanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DecisionPlanDto>> GetDecision(
        [FromQuery] PlanScenarioQuery scenario,
        [FromQuery] PayoffRolloverKind kind,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _savedPlanService.GetPreviewAsync(scenario, kind, cancellationToken));
    }

    /// <summary>
    /// GET /api/plan/versions
    /// Returns the household's saved plan versions, newest first.
    /// </summary>
    [HttpGet("versions")]
    [ProducesResponseType<IReadOnlyList<PlanVersionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PlanVersionDto>>> GetVersions(
        CancellationToken cancellationToken = default)
    {
        return Ok(await _savedPlanService.ListAsync(cancellationToken));
    }

    /// <summary>
    /// GET /api/plan/versions/{id}
    /// Returns one saved plan version for this household.
    /// </summary>
    [HttpGet("versions/{id:guid}")]
    [ProducesResponseType<PlanVersionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanVersionDto>> GetVersion(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _savedPlanService.GetAsync(id, cancellationToken));
    }

    /// <summary>
    /// POST /api/plan/versions
    /// Saves the current decision plan as a draft. A matching open draft is returned again.
    /// A stale draft id is rejected. No money is moved.
    /// </summary>
    [HttpPost("versions")]
    [ProducesResponseType<PlanVersionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlanVersionDto>> SaveVersion(
        [FromBody] SavePlanVersionDto dto,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _savedPlanService.SaveDraftAsync(dto, cancellationToken));
    }

    /// <summary>
    /// POST /api/plan/versions/{id}/accept
    /// Accepts that draft and supersedes the previous accepted plan.
    /// Accepting the plan that is already accepted returns it. No money is moved.
    /// </summary>
    [HttpPost("versions/{id:guid}/accept")]
    [ProducesResponseType<PlanVersionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlanVersionDto>> AcceptVersion(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _savedPlanService.AcceptAsync(id, cancellationToken));
    }
}
