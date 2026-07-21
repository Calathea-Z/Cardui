namespace Cardui.Api.Models;

public class Group
{
    public Guid Id { get; init; }

    public required string Key { get; init; }
    public required string Name { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<SubGroup> SubGroups { get; init; } = new List<SubGroup>();
}
