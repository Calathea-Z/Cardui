namespace Cardui.Api.Dtos.Category;

public class CategoryDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Key { get; set; }
    public Guid SubGroupId { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public bool IsSystem { get; set; }
}
