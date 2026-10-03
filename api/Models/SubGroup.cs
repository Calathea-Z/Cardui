namespace Cardui.Api.Models;

public class SubGroup
{
    public Guid Id { get; init; }

    public Guid? HouseholdId { get; init; }

    public Guid GroupId { get; set; }
    public Group Group { get; init; } = null!;

    public required string Key { get; init; }
    public required string Name { get; set; }

    public bool IsSystem { get; init; }
    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Category> Categories { get; init; } = new List<Category>();
}
