namespace Cardui.Api.Models;

public class Category
{
    public Guid Id { get; init; }

    public Guid? HouseholdId { get; init; }

    public required string Key { get; init; }
    public required string Name { get; set; }

    public Guid SubGroupId { get; set; }
    public SubGroup SubGroup { get; init; } = null!;

    public string? Color { get; set; }
    public string? Icon { get; set; }

    public bool IsSystem { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Transaction> Transactions { get; init; } = new List<Transaction>();
}
