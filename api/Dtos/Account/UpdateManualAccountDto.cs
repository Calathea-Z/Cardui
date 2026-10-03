using System.ComponentModel.DataAnnotations;

namespace Cardui.Api.Dtos.Account;

public class UpdateManualAccountDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = "";

    [Required]
    [StringLength(100)]
    public string Type { get; set; } = "";

    [StringLength(100)]
    public string? Subtype { get; set; }

    [StringLength(20)]
    public string? Mask { get; set; }

    [StringLength(10)]
    public string? IsoCurrencyCode { get; set; }

    public decimal OpeningBalance { get; set; }

    public DateOnly OpeningBalanceDate { get; set; }
}
