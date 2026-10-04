using Cardui.Api.Dtos.Household;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cardui.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/households")]
public class HouseholdsController : ControllerBase
{
    private readonly IHouseholdsService _householdsService;
    private readonly IFinancialProfileService _financialProfileService;
    private readonly IHouseholdOwnerContext _ownerContext;

    public HouseholdsController(
        IHouseholdsService householdsService,
        IFinancialProfileService financialProfileService,
        IHouseholdOwnerContext ownerContext)
    {
        _householdsService = householdsService;
        _financialProfileService = financialProfileService;
        _ownerContext = ownerContext;
    }

    /// <summary>
    /// POST /api/households/current
    /// Returns the signed-in owner's household, creating it on the first call.
    /// This route is excluded from household-scope middleware so the household
    /// can be created before later requests are scoped.
    /// </summary>
    [HttpPost("current")]
    [ProducesResponseType<HouseholdDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<HouseholdDto>> GetOrCreateCurrent(
        CancellationToken cancellationToken)
    {
        var household = await _householdsService.GetOrCreateForOwnerAsync(
            _ownerContext.ClerkUserId,
            cancellationToken);
        return Ok(household);
    }

    [HttpGet("current/profile")]
    [ProducesResponseType<FinancialProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FinancialProfileDto>> GetProfile(
        CancellationToken cancellationToken)
    {
        return Ok(await _financialProfileService.GetAsync(cancellationToken));
    }

    [HttpPut("current/profile")]
    [ProducesResponseType<FinancialProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FinancialProfileDto>> UpdateProfile(
        [FromBody] UpdateFinancialProfileDto dto,
        CancellationToken cancellationToken)
    {
        return Ok(await _financialProfileService.UpdateAsync(dto, cancellationToken));
    }

    [HttpPost("current/contributors")]
    [ProducesResponseType<HouseholdContributorDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<HouseholdContributorDto>> AddContributor(
        [FromBody] UpsertHouseholdContributorDto dto,
        CancellationToken cancellationToken)
    {
        return Ok(await _financialProfileService.AddContributorAsync(dto, cancellationToken));
    }

    [HttpPut("current/contributors/{contributorId:guid}")]
    [ProducesResponseType<HouseholdContributorDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HouseholdContributorDto>> UpdateContributor(
        Guid contributorId,
        [FromBody] UpsertHouseholdContributorDto dto,
        CancellationToken cancellationToken)
    {
        return Ok(await _financialProfileService.UpdateContributorAsync(
            contributorId,
            dto,
            cancellationToken));
    }

    [HttpDelete("current/contributors/{contributorId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveContributor(
        Guid contributorId,
        CancellationToken cancellationToken)
    {
        await _financialProfileService.RemoveContributorAsync(
            contributorId,
            cancellationToken);
        return NoContent();
    }
}
