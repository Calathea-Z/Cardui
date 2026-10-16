using Cardui.Api.Dtos.Allocation;
using Cardui.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cardui.Api.Controllers;

[ApiController]
[Route("api/allocation")]
public class AllocationController : ControllerBase
{
    private readonly IAllocationService _allocationService;

    public AllocationController(IAllocationService allocationService)
    {
        _allocationService = allocationService;
    }

    /// <summary>
    /// GET /api/allocation
    /// Returns the household's variable-income and windfall allocation preference.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<AllocationPageDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AllocationPageDto>> GetAllocation(
        CancellationToken cancellationToken = default)
    {
        return Ok(await _allocationService.GetAsync(cancellationToken));
    }

    /// <summary>
    /// PUT /api/allocation
    /// Stores both allocation rules. No money is moved.
    /// </summary>
    [HttpPut]
    [ProducesResponseType<AllocationPageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AllocationPageDto>> SaveAllocation(
        [FromBody] UpsertAllocationPreferenceDto dto,
        CancellationToken cancellationToken)
    {
        return Ok(await _allocationService.SaveAsync(dto, cancellationToken));
    }
}
