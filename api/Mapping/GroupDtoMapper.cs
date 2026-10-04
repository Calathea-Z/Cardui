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
    /// Maps a group and its sub-groups, ordered for display.
    /// </summary>
    public static GroupDetailDto MapToDetailDto(Group group)
    {
        return new GroupDetailDto
        {
            Id = group.Id,
            Key = group.Key,
            Name = group.Name,
            SortOrder = group.SortOrder,
            SubGroups = group.SubGroups
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(SubGroupDtoMapper.MapToDto)
                .ToList()
        };
    }
}
