namespace Cardui.Api.Domain.Living;

/// <summary>
/// One person's current monthly benchmark for pay available to the shared plan.
/// MonthlyAmount is null when they share all recorded pay. The calculator derives a share that also applies to low pay and raises.
/// </summary>
public sealed record ContributionShareInput(
    Guid ContributorId,
    string Name,
    decimal? MonthlyAmount);
