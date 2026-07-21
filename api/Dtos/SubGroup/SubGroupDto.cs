namespace Cardui.Api.Dtos.SubGroup;

public class SubGroupDto
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public required string Key { get; set; }
    public required string Name { get; set; }
    public bool IsSystem { get; set; }
    public int SortOrder { get; set; }
}
