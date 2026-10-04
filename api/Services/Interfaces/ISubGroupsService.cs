using Cardui.Api.Dtos.SubGroup;

namespace Cardui.Api.Services.Interfaces;

public interface ISubGroupsService
{
    /// <summary>
    /// Lists system sub-groups and this household's sub-groups.
    /// Pass a group id to limit the list to that group.
    /// </summary>
    Task<IReadOnlyList<SubGroupDto>> GetSubGroupsAsync(
        Guid? groupId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns one sub-group the household is allowed to see.
    /// </summary>
    Task<SubGroupDto> GetSubGroupByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a household sub-group at the end of a group's sort order.
    /// The name and derived key must be unique among system rows and this household.
    /// </summary>
    Task<SubGroupDto> CreateSubGroupAsync(
        CreateSubGroupDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renames a household sub-group. System sub-groups cannot be renamed.
    /// </summary>
    Task<SubGroupDto> UpdateSubGroupAsync(
        Guid id,
        UpdateSubGroupDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a household sub-group that has no categories.
    /// System sub-groups cannot be deleted.
    /// </summary>
    Task DeleteSubGroupAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
