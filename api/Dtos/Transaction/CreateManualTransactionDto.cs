using System.ComponentModel.DataAnnotations;

namespace Cardui.Api.Dtos.Transaction;

public class CreateManualTransactionDto
{
    public Guid AccountId { get; set; }

    public DateOnly Date { get; set; }

    [Required]
    [StringLength(300)]
    public string Name { get; set; } = "";

    public decimal Amount { get; set; }

    public Guid? CategoryId { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public bool Pending { get; set; }
}
