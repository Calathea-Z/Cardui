namespace Cardui.Api.Models;

public class CategoryTargetMonth
{
    public Guid Id { get; init; }

    public Guid HouseholdId { get; set; }

    public Household? Household { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public int? CopiedFromYear { get; set; }

    public int? CopiedFromMonth { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public ICollection<CategoryTarget> Targets { get; init; } = new List<CategoryTarget>();
}
