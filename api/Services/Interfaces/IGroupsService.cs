using Cardui.Api.Dtos.Group;

namespace Cardui.Api.Services.Interfaces;

public interface IGroupsService
{
    Task<IReadOnlyList<GroupDto>> GetGroupsAsync(
        CancellationToken cancellationToken = default);

    Task<GroupDetailDto> GetGroupByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
