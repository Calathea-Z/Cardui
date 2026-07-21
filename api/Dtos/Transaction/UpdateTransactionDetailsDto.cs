using System.ComponentModel.DataAnnotations;

namespace Cardui.Api.Dtos.Transaction;

public class UpdateTransactionDetailsDto
{
    public DateOnly Date { get; set; }

    public Guid? CategoryId { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
