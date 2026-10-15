namespace Cardui.Api.Domain.Allocation;

/// <summary>
/// How one amount would be treated. The three parts add back to that amount.
/// This is a review of the saved rule. It is not a transfer.
/// </summary>
public sealed record AllocationSplit(
    decimal Debt,
    decimal Savings,
    decimal Cash);
