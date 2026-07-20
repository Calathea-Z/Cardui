using System.ComponentModel.DataAnnotations;

namespace Cardui.Api.Dtos.Plaid;

public class ExchangePublicTokenRequestDto
{
    [Required]
    [StringLength(1_000)]
    public required string PublicToken { get; set; }

    [StringLength(200)]
    public string? InstitutionId { get; set; }

    [StringLength(200)]
    public string? InstitutionName { get; set; }
}
