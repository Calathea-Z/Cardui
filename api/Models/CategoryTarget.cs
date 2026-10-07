namespace Cardui.Api.Models;

public class CategoryTarget
{
    public Guid Id { get; init; }

    public Guid CategoryTargetMonthId { get; set; }

    public CategoryTargetMonth Month { get; set; } = null!;

    public Guid CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public decimal Amount { get; set; }

    public bool Rollover { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }
}
