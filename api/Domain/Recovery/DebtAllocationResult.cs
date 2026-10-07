namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// One round of minimums plus extra, in the order the caller supplied.
/// ExtraApplied is the part of the pool that reduced a balance. ExtraLeft was not applied to any debt.
/// The order is not an avalanche ranking. A paid-off debt's minimum is not passed to the next debt.
/// </summary>
public sealed record DebtAllocationResult(
    IReadOnlyList<DebtAllocationLine> Lines,
    decimal ExtraApplied,
    decimal ExtraLeft);
