using System.Linq.Expressions;
using Cardui.Api.Dtos.SubGroup;
using Cardui.Api.Models;

namespace Cardui.Api.Mapping;

public static class SubGroupDtoMapper
{
    public static readonly Expression<Func<SubGroup, SubGroupDto>> Projection = x => new SubGroupDto
    {
        Id = x.Id,
        GroupId = x.GroupId,
        Key = x.Key,
        Name = x.Name,
        IsSystem = x.IsSystem,
        SortOrder = x.SortOrder
    };

    /// <summary>
    /// Maps a tracked sub-group to the API response. Used after create and update.
    /// </summary>
    public static SubGroupDto MapToDto(SubGroup subGroup)
    {
        return new SubGroupDto
        {
            Id = subGroup.Id,
            GroupId = subGroup.GroupId,
            Key = subGroup.Key,
            Name = subGroup.Name,
            IsSystem = subGroup.IsSystem,
            SortOrder = subGroup.SortOrder
        };
    }
}
