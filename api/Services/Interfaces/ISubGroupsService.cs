using Cardui.Api.Dtos.SubGroup;

namespace Cardui.Api.Services.Interfaces;

public interface ISubGroupsService
{
    Task<IReadOnlyList<SubGroupDto>> GetSubGroupsAsync(
        Guid? groupId = null,
        CancellationToken cancellationToken = default);

    Task<SubGroupDto> GetSubGroupByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<SubGroupDto> CreateSubGroupAsync(
        CreateSubGroupDto dto,
        CancellationToken cancellationToken = default);

    Task<SubGroupDto> UpdateSubGroupAsync(
        Guid id,
        UpdateSubGroupDto dto,
        CancellationToken cancellationToken = default);

    Task DeleteSubGroupAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
