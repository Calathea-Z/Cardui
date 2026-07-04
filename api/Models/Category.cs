namespace Cardui.Api.Models;

public class Category
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public Guid? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }

    public string? Color { get; set; }
    public string? Icon { get; set; }

    public bool IsSystem { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Category> ChildCategories { get; set; } = new List<Category>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}