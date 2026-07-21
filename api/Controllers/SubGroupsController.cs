using Cardui.Api.Dtos.SubGroup;
using Cardui.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cardui.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubGroupsController : ControllerBase
{
    private readonly ISubGroupsService _subGroupsService;

    public SubGroupsController(ISubGroupsService subGroupsService)
    {
        _subGroupsService = subGroupsService;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SubGroupDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SubGroupDto>>> GetSubGroups(
        [FromQuery] Guid? groupId,
        CancellationToken cancellationToken)
    {
        var subGroups = await _subGroupsService.GetSubGroupsAsync(
            groupId,
            cancellationToken);
        return Ok(subGroups);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<SubGroupDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubGroupDto>> GetSubGroupById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var subGroup = await _subGroupsService.GetSubGroupByIdAsync(
            id,
            cancellationToken);
        return Ok(subGroup);
    }

    [HttpPost]
    [ProducesResponseType<SubGroupDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubGroupDto>> CreateSubGroup(
        [FromBody] CreateSubGroupDto dto,
        CancellationToken cancellationToken)
    {
        var subGroup = await _subGroupsService.CreateSubGroupAsync(
            dto,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetSubGroupById),
            new { id = subGroup.Id },
            subGroup);
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType<SubGroupDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubGroupDto>> UpdateSubGroup(
        Guid id,
        [FromBody] UpdateSubGroupDto dto,
        CancellationToken cancellationToken)
    {
        var subGroup = await _subGroupsService.UpdateSubGroupAsync(
            id,
            dto,
            cancellationToken);
        return Ok(subGroup);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSubGroup(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _subGroupsService.DeleteSubGroupAsync(id, cancellationToken);
        return NoContent();
    }
}
