using System.Linq.Expressions;
using Cardui.Api.Dtos.Group;
using Cardui.Api.Dtos.SubGroup;
using Cardui.Api.Models;

namespace Cardui.Api.Mapping;

public static class GroupDtoMapper
{
    public static readonly Expression<Func<Group, GroupDto>> Projection = x => new GroupDto
    {
        Id = x.Id,
        Key = x.Key,
        Name = x.Name,
        SortOrder = x.SortOrder
    };

    /// <summary>
    /// Maps a group and the sub-groups the caller already filtered, ordered for display.
    /// </summary>
    public static GroupDetailDto MapToDetailDto(
        Group group,
        IEnumerable<SubGroup> subGroups)
    {
        return new GroupDetailDto
        {
            Id = group.Id,
            Key = group.Key,
            Name = group.Name,
            SortOrder = group.SortOrder,
            SubGroups = subGroups
                .OrderBy(subGroup => subGroup.SortOrder)
                .ThenBy(subGroup => subGroup.Name)
                .Select(SubGroupDtoMapper.MapToDto)
                .ToList()
        };
    }
}
