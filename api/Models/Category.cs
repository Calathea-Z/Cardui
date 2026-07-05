namespace Cardui.Api.Models;

public class Category
{
    public Guid Id { get; init; }
    public required string Key { get; init; }
    public required string Name { get; set; }


    public Guid? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; init; }

    public string? Color { get; set; }
    public string? Icon { get; set; }

    public bool IsSystem { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Category> ChildCategories { get; init; } = new List<Category>();
    public ICollection<Transaction> Transactions { get; init; } = new List<Transaction>();
}