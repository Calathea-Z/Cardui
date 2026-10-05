using Cardui.Api.Models;

namespace Cardui.Api.Dtos.Income;

public class UpsertIncomeSourceDto
{
    public string? Name { get; set; }

    public decimal TakeHomeAmount { get; set; }

    public decimal? LowTakeHomeAmount { get; set; }

    public decimal? StrongTakeHomeAmount { get; set; }

    public IncomeCadence? Cadence { get; set; }

    public DateOnly NextPaymentDate { get; set; }

    public Guid? ContributorId { get; set; }

    public IncomeReliability? Reliability { get; set; }

    public IReadOnlyList<UpsertIncomeRaiseDto>? Raises { get; set; }
}
