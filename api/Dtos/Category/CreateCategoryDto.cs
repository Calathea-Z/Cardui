namespace Cardui.Api.Dtos.Category;

public class CreateCategoryDto
{
    public required string Name { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
}