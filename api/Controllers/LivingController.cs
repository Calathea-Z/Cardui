using Cardui.Api.Dtos.Living;
using Cardui.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cardui.Api.Controllers;

[ApiController]
[Route("api/living")]
public class LivingController : ControllerBase
{
    private readonly ILivingService _livingService;

    public LivingController(ILivingService livingService)
    {
        _livingService = livingService;
    }

    /// <summary>
    /// GET /api/living
    /// Returns contribution shares, monthly living spending, and the affordability gap.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<LivingPageDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LivingPageDto>> GetLiving(
        CancellationToken cancellationToken = default)
    {
        return Ok(await _livingService.GetAsync(cancellationToken));
    }

    /// <summary>
    /// PUT /api/living/contributions/{contributorId}
    /// Stores how much of that person's pay the shared plan may use in a month.
    /// </summary>
    [HttpPut("contributions/{contributorId:guid}")]
    [ProducesResponseType<LivingPageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LivingPageDto>> SetContribution(
        Guid contributorId,
        [FromBody] UpsertLivingContributionDto dto,
        CancellationToken cancellationToken)
    {
        return Ok(await _livingService.SetContributionAsync(contributorId, dto, cancellationToken));
    }

}
