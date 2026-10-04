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
    private readonly IHouseholdOwnerContext _ownerContext;

    public HouseholdsController(
        IHouseholdsService householdsService,
        IHouseholdOwnerContext ownerContext)
    {
        _householdsService = householdsService;
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
}
