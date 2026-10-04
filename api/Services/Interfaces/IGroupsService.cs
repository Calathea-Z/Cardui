using Cardui.Api.Dtos.Group;

namespace Cardui.Api.Services.Interfaces;

public interface IGroupsService
{
    /// <summary>
    /// Lists category groups in display order. Groups are shared system data.
    /// </summary>
    Task<IReadOnlyList<GroupDto>> GetGroupsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns one group and the sub-groups this household can see.
    /// Another household's custom sub-groups are left out.
    /// </summary>
    Task<GroupDetailDto> GetGroupByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
