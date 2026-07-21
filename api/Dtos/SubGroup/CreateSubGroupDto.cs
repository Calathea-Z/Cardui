using System.ComponentModel.DataAnnotations;

namespace Cardui.Api.Dtos.SubGroup;

public class CreateSubGroupDto
{
    [Required]
    public Guid GroupId { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; set; }
}
