namespace Cardui.Api.Domain.Living;

/// <summary>
/// How one person's pay enters the shared plan.
/// RecordedMonthly is the average of their scheduled pay in the planning currency, and it is null when they have none.
/// SharedMonthly is the part the plan uses. KeptMonthly stays with them.
/// HasUnscheduledPay is true when a paycheck has no schedule and stays on its own date.
/// </summary>
public sealed record ContributionPerson(
    Guid ContributorId,
    string Name,
    decimal? MonthlyAmount,
    decimal? RecordedMonthly,
    decimal SharedMonthly,
    decimal KeptMonthly,
    ContributionLimit Limit,
    bool HasUnscheduledPay);
