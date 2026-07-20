using System.ComponentModel.DataAnnotations;

namespace Cardui.Api.Dtos.Category;

public class UpdateCategoryDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; set; }

    public Guid? ParentCategoryId { get; set; }

    [StringLength(20)]
    public string? Color { get; set; }

    [StringLength(100)]
    public string? Icon { get; set; }
}
