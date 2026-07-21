namespace Cardui.Api.Dtos.Group;

public class GroupDto
{
    public Guid Id { get; set; }
    public required string Key { get; set; }
    public required string Name { get; set; }
    public int SortOrder { get; set; }
}
