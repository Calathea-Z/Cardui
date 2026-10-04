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

    /// <summary>
    /// GET /api/subgroups
    /// Lists visible sub-groups. groupId limits the list to one group.
    /// </summary>
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

    /// <summary>
    /// GET /api/subgroups/{id}
    /// Returns one visible sub-group.
    /// </summary>
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

    /// <summary>
    /// POST /api/subgroups
    /// Creates a household sub-group inside a group.
    /// </summary>
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

    /// <summary>
    /// PATCH /api/subgroups/{id}
    /// Renames a household sub-group.
    /// </summary>
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

    /// <summary>
    /// DELETE /api/subgroups/{id}
    /// Deletes a household sub-group that has no categories.
    /// </summary>
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
