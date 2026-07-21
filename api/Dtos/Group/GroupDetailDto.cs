using Cardui.Api.Dtos.SubGroup;

namespace Cardui.Api.Dtos.Group;

public class GroupDetailDto
{
    public Guid Id { get; set; }
    public required string Key { get; set; }
    public required string Name { get; set; }
    public int SortOrder { get; set; }
    public IReadOnlyList<SubGroupDto> SubGroups { get; set; } = [];
}
