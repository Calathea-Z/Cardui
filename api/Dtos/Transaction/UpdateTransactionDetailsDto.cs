using System.ComponentModel.DataAnnotations;

namespace Cardui.Api.Dtos.Transaction;

public class UpdateTransactionDetailsDto
{
    public DateOnly Date { get; set; }

    public Guid? CategoryId { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [StringLength(300)]
    public string? Name { get; set; }

    public decimal? Amount { get; set; }

    public bool? Pending { get; set; }
}
