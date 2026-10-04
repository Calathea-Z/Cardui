using Cardui.Api.Dtos.Group;
using Cardui.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cardui.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GroupsController : ControllerBase
{
    private readonly IGroupsService _groupsService;

    public GroupsController(IGroupsService groupsService)
    {
        _groupsService = groupsService;
    }

    /// <summary>
    /// GET /api/groups
    /// Lists category groups in display order.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<GroupDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GroupDto>>> GetGroups(
        CancellationToken cancellationToken)
    {
        var groups = await _groupsService.GetGroupsAsync(cancellationToken);
        return Ok(groups);
    }

    /// <summary>
    /// GET /api/groups/{id}
    /// Returns one group and the sub-groups this household can see.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<GroupDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupDetailDto>> GetGroupById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var group = await _groupsService.GetGroupByIdAsync(id, cancellationToken);
        return Ok(group);
    }
}
