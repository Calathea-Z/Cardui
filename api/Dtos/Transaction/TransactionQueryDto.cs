using System.ComponentModel.DataAnnotations;

namespace Cardui.Api.Dtos.Transaction;

public class TransactionQueryDto
{
    [StringLength(100)]
    public string? Search { get; set; }

    public Guid? AccountId { get; set; }
    public Guid? CategoryId { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public bool? Pending { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 50;
}
