using Cardui.Api.Models;

namespace Cardui.Api.Dtos.Income;

public class IncomeSourceDto
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public decimal TakeHomeAmount { get; set; }

    public decimal? LowTakeHomeAmount { get; set; }

    public decimal? StrongTakeHomeAmount { get; set; }

    public decimal? GrossPayAmount { get; set; }

    public required string Currency { get; set; }

    public IncomeCadence Cadence { get; set; }

    public DateOnly NextPaymentDate { get; set; }

    public Guid? ContributorId { get; set; }

    public string? ContributorName { get; set; }

    public IncomeReliability Reliability { get; set; }

    public IReadOnlyList<IncomeRaiseDto> Raises { get; set; } = [];

    public IReadOnlyList<DateOnly> UpcomingPaymentDates { get; set; } = [];

    public decimal? AverageMonthlyAmount { get; set; }
}
