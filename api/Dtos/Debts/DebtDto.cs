using Cardui.Api.Domain;
using Cardui.Api.Domain.Debts;
using Cardui.Api.Models;

namespace Cardui.Api.Dtos.Debts;

public class DebtDto
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public DebtKind Kind { get; set; }

    public Guid? AccountId { get; set; }

    public string? AccountName { get; set; }

    /// <summary>
    /// True when the debt follows its linked account. A reference link stays false.
    /// </summary>
    public bool Following { get; set; }

    /// <summary>
    /// The balance stored on the debt. While following without an override, the amount in use is BalanceInUse.
    /// </summary>
    public decimal? Balance { get; set; }

    public DateOnly? BalanceAsOf { get; set; }

    /// <summary>
    /// The balance the plan uses. On a followed debt this is the connected balance, except where the person kept their own.
    /// </summary>
    public decimal? BalanceInUse { get; set; }

    public DateOnly? BalanceInUseAsOf { get; set; }

    public DebtFieldSource BalanceSource { get; set; }

    /// <summary>
    /// The account's latest balance. Null when the debt is not following or the account has no snapshot.
    /// </summary>
    public decimal? SyncedBalance { get; set; }

    public DateOnly? SyncedBalanceAsOf { get; set; }

    public DebtAccountBalanceBlock SyncedBalanceBlock { get; set; }

    /// <summary>
    /// The positive credit on a followed card, counted as zero in BalanceInUse. Null when the balance is not a credit.
    /// </summary>
    public decimal? BalanceCredit { get; set; }

    /// <summary>
    /// How current the connection is. Null when the debt is not following.
    /// </summary>
    public DebtLinkFreshness? Freshness { get; set; }

    /// <summary>
    /// The day the last sync failed, in the household time zone. Null unless freshness is sync failing.
    /// </summary>
    public DateOnly? SyncFailedOn { get; set; }

    public required string Currency { get; set; }

    public decimal? Apr { get; set; }

    public decimal? MinimumPayment { get; set; }

    public DateOnly? NextDueDate { get; set; }

    public decimal? CreditLimit { get; set; }

    public int? RemainingTermMonths { get; set; }

    public decimal? PromotionalApr { get; set; }

    public DateOnly? PromotionalEndsOn { get; set; }

    /// <summary>
    /// Share of the credit limit in use, as a ratio. 0.85 means 85 percent.
    /// Null when the balance or the credit limit is unknown. This is calculated and is not stored.
    /// </summary>
    public decimal? Utilization { get; set; }
}
