using Cardui.Api.Domain.Debts;

namespace Cardui.Api.Dtos.Debts;

/// <summary>
/// A connected account a debt is allowed to follow.
/// BalanceInUse is the amount following would use before an override.
/// BalancesDiffer is true when the debt already has a different balance the person can keep.
/// BalanceCredit is the positive credit counted as zero. It is null when the balance is not a credit.
/// </summary>
public class DebtFollowAccountDto
{
    public Guid AccountId { get; set; }

    public required string Name { get; set; }

    public string? Mask { get; set; }

    public decimal? SyncedBalance { get; set; }

    public DateOnly? SyncedBalanceAsOf { get; set; }

    public decimal? BalanceInUse { get; set; }

    public DateOnly? BalanceInUseAsOf { get; set; }

    public DebtAccountBalanceBlock Block { get; set; }

    public decimal? BalanceCredit { get; set; }

    public bool BalancesDiffer { get; set; }
}
