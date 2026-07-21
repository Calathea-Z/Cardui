using System.ComponentModel.DataAnnotations;

namespace Cardui.Api.Dtos.SubGroup;

public class UpdateSubGroupDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; set; }
}
