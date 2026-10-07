using Cardui.Api.Domain.Accounts;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.Debts;

public static class DebtSummary
{
    public const decimal AprNoticePercent = 20m;

    public const decimal UtilizationNotice = 0.30m;

    public const decimal UtilizationLimitNotice = 0.90m;

    public const int PromotionalNoticeDays = 60;

    /// <summary>
    /// Builds the inventory summary for the debts a household has recorded.
    /// Interest, minimums, and utilization use each debt's dated balance.
    /// A linked account balance is reported beside it when the amounts differ, and it is not used until chosen.
    /// Unknown terms stay out of the totals. The result has no score.
    /// </summary>
    public static DebtSummaryReport Calculate(
        IReadOnlyList<DebtSummaryInput> debts,
        DateOnly today)
    {
        var described = debts
            .Select(debt => (Input: debt, Item: Describe(debt, today)))
            .ToList();
        var items = described.Select(pair => pair.Item).ToList();
        var currencies = described
            .GroupBy(pair => pair.Item.Currency, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => Summarize(group.Key, group.ToList()))
            .ToList();

        return new DebtSummaryReport(
            AprNoticePercent,
            UtilizationNotice,
            UtilizationLimitNotice,
            PromotionalNoticeDays,
            currencies,
            items);
    }

    /// <summary>
    /// Reads the linked liability balance a person chose to store on the debt.
    /// The account itself is not changed. A cash balance, a missing date, or a different currency is rejected.
    /// </summary>
    public static bool TryReadChosenBalance(
        string debtCurrency,
        DebtLinkedBalance? linked,
        out decimal balance,
        out DateOnly asOf,
        out string error)
    {
        balance = 0;
        asOf = default;
        if (linked is not DebtLinkedBalance account)
        {
            error = "This debt is not linked to an account.";
            return false;
        }

        if (!account.IsLiability)
        {
            error = "That account balance is not an amount owed, so it is not copied.";
            return false;
        }

        var block = Classify(debtCurrency, account);
        if (block != DebtAccountBalanceBlock.None)
        {
            error = BlockMessage(block);
            return false;
        }

        balance = AccountLedger.Round(account.Balance);
        asOf = account.AsOf!.Value;
        error = "";
        return true;
    }

    /// <summary>
    /// Decides whether a linked balance can be stored on a debt.
    /// A missing date, a negative amount, a different currency, or an amount above the stored limit cannot.
    /// </summary>
    public static DebtAccountBalanceBlock ClassifyLinkedBalance(
        string debtCurrency,
        DebtLinkedBalance account)
    {
        return Classify(debtCurrency, account);
    }

    #region Private Methods

    /// <summary>
    /// Describes one debt on the given day.
    /// The promotional rate is the rate in effect through its end date, including that date.
    /// </summary>
    private static DebtSummaryItem Describe(DebtSummaryInput debt, DateOnly today)
    {
        var (rate, promotional) = RateInEffect(
            debt.Apr,
            debt.PromotionalApr,
            debt.PromotionalEndsOn,
            today);
        var promotionEnded = debt.PromotionalEndsOn is DateOnly end && end < today;
        var promoEndsWithinNotice = debt.PromotionalEndsOn is DateOnly promoEnd
            && promoEnd >= today
            && promoEnd <= today.AddDays(PromotionalNoticeDays);
        var utilization = debt.Kind == DebtKind.Revolving
            ? DebtRules.Utilization(debt.Balance, debt.CreditLimit)
            : null;

        return new DebtSummaryItem(
            debt.DebtId,
            debt.Currency,
            MonthlyInterest(debt.Balance, rate),
            rate,
            promotional,
            promotionEnded,
            debt.PromotionalEndsOn,
            promoEndsWithinNotice,
            debt.NextDueDate is DateOnly due && due < today,
            rate is decimal apr && apr >= AprNoticePercent,
            utilization is decimal notice
                && notice >= UtilizationNotice
                && notice < UtilizationLimitNotice,
            utilization is decimal nearLimit && nearLimit >= UtilizationLimitNotice,
            Gaps(debt, rate, promotional),
            CompareBalances(debt, debt.LinkedBalance));
    }

    /// <summary>
    /// Adds the known amounts for one currency and counts what is still missing.
    /// An unknown amount is left out of the total. It is not stored as zero.
    /// </summary>
    private static DebtCurrencySummary Summarize(
        string currency,
        IReadOnlyList<(DebtSummaryInput Input, DebtSummaryItem Item)> pairs)
    {
        var items = pairs.Select(pair => pair.Item).ToList();
        var (recordedBalance, unknownBalances) = SumKnown(
            pairs.Select(pair => pair.Input.Balance));
        var (monthlyInterest, unknownInterest) = SumKnown(
            items.Select(item => item.MonthlyInterest));
        var (minimums, unknownMinimums) = SumKnown(
            pairs.Select(pair => pair.Input.MinimumPayment));
        var (utilization, utilizationCount, unknownUtilization) = CombinedUtilization(pairs);

        return new DebtCurrencySummary(
            currency,
            items.Count,
            recordedBalance,
            unknownBalances,
            monthlyInterest,
            unknownInterest,
            minimums,
            unknownMinimums,
            utilization,
            utilizationCount,
            unknownUtilization,
            items.Count(item => item.DueDatePassed),
            items.Count(item => item.PromoEndsWithinNotice),
            items.Count(item => item.PromotionEnded),
            items.Count(item => item.AprReachesNotice),
            items.Count(item => item.UtilizationReachesNotice),
            items.Count(item => item.UtilizationReachesLimitNotice),
            items.Count(item => item.BalanceComparison is not null),
            GapCount(items, DebtSummaryGap.DueDate),
            GapCount(items, DebtSummaryGap.RemainingTerm),
            GapCount(items, DebtSummaryGap.PromotionalEnd),
            GapCount(items, DebtSummaryGap.PromotionalRate),
            GapCount(items, DebtSummaryGap.RateAfterPromotion),
            pairs.Count(pair => pair.Input.Freshness is DebtLinkFreshness freshness
                && freshness != DebtLinkFreshness.Current));
    }

    /// <summary>
    /// One month of simple interest on the recorded balance.
    /// The amount is the balance times the rate, divided by 12, rounded to cents.
    /// Null when the balance or the rate is unknown. It is not the total interest left to pay.
    /// </summary>
    private static decimal? MonthlyInterest(decimal? balance, decimal? ratePercent)
    {
        if (balance is not decimal amount || ratePercent is not decimal rate)
        {
            return null;
        }

        return AccountLedger.Round(amount * rate / 100m / 12m);
    }

    /// <summary>
    /// The rate used for this month's interest.
    /// A promotional APR applies when it is known and its end date is blank, today, or later.
    /// After that date, the regular APR is used, and a missing regular APR stays unknown.
    /// </summary>
    private static (decimal? Rate, bool Promotional) RateInEffect(
        decimal? apr,
        decimal? promotionalApr,
        DateOnly? promotionalEndsOn,
        DateOnly today)
    {
        if (promotionalApr is not null
            && (promotionalEndsOn is null || promotionalEndsOn >= today))
        {
            return (promotionalApr, true);
        }

        return (apr, false);
    }

    /// <summary>
    /// Lists the terms this debt still does not have.
    /// A credit limit is a gap only on a revolving debt. Months left are a gap only on an installment debt.
    /// </summary>
    private static IReadOnlyList<DebtSummaryGap> Gaps(
        DebtSummaryInput debt,
        decimal? rateInEffect,
        bool rateIsPromotional)
    {
        var gaps = new List<DebtSummaryGap>();
        if (debt.Balance is null)
        {
            gaps.Add(DebtSummaryGap.Balance);
        }

        if (rateInEffect is null)
        {
            gaps.Add(DebtSummaryGap.Apr);
        }

        if (debt.MinimumPayment is null)
        {
            gaps.Add(DebtSummaryGap.MinimumPayment);
        }

        if (debt.NextDueDate is null)
        {
            gaps.Add(DebtSummaryGap.DueDate);
        }

        if (debt.Kind == DebtKind.Revolving && debt.CreditLimit is null)
        {
            gaps.Add(DebtSummaryGap.CreditLimit);
        }

        if (debt.Kind == DebtKind.Installment && debt.RemainingTermMonths is null)
        {
            gaps.Add(DebtSummaryGap.RemainingTerm);
        }

        if (debt.PromotionalApr is not null && debt.PromotionalEndsOn is null)
        {
            gaps.Add(DebtSummaryGap.PromotionalEnd);
        }

        if (debt.PromotionalEndsOn is not null && debt.PromotionalApr is null)
        {
            gaps.Add(DebtSummaryGap.PromotionalRate);
        }

        if (rateIsPromotional && debt.Apr is null)
        {
            gaps.Add(DebtSummaryGap.RateAfterPromotion);
        }

        return gaps;
    }

    /// <summary>
    /// Sets a linked card or loan balance beside the debt when the amounts differ.
    /// A followed debt already uses its resolved balance, so it is not a second choice.
    /// A cash account is not the debt's balance. Matching amounts are not a difference.
    /// </summary>
    private static DebtBalanceComparison? CompareBalances(
        DebtSummaryInput debt,
        DebtLinkedBalance? linked)
    {
        if (debt.Following || linked is not { IsLiability: true } account)
        {
            return null;
        }

        var accountBalance = AccountLedger.Round(account.Balance);
        decimal? recorded = debt.Balance is decimal value
            ? AccountLedger.Round(value)
            : null;
        if (recorded == accountBalance)
        {
            return null;
        }

        var block = Classify(debt.Currency, account);
        return new DebtBalanceComparison(
            accountBalance,
            account.AsOf,
            NormalizeCurrency(account.Currency),
            block == DebtAccountBalanceBlock.None,
            block);
    }

    /// <summary>
    /// Decides whether the linked balance can be stored on the debt.
    /// A missing date, a negative amount, a different currency, or an amount above the stored limit cannot.
    /// </summary>
    private static DebtAccountBalanceBlock Classify(
        string debtCurrency,
        DebtLinkedBalance account)
    {
        var accountBalance = AccountLedger.Round(account.Balance);
        if (account.AsOf is null)
        {
            return DebtAccountBalanceBlock.DateUnknown;
        }

        if (accountBalance < 0)
        {
            return DebtAccountBalanceBlock.NegativeBalance;
        }

        if (accountBalance > DebtRules.MaxAmount)
        {
            return DebtAccountBalanceBlock.AmountTooLarge;
        }

        var accountCurrency = NormalizeCurrency(account.Currency);
        var currency = NormalizeCurrency(debtCurrency);
        if (accountCurrency is null
            || currency is null
            || !string.Equals(accountCurrency, currency, StringComparison.OrdinalIgnoreCase))
        {
            return DebtAccountBalanceBlock.CurrencyDiffers;
        }

        return DebtAccountBalanceBlock.None;
    }

    /// <summary>
    /// The sentence returned when a person asks to copy a balance that cannot be stored.
    /// </summary>
    private static string BlockMessage(DebtAccountBalanceBlock block)
    {
        return block switch
        {
            DebtAccountBalanceBlock.DateUnknown =>
                "That account balance has no date, so it is not copied.",
            DebtAccountBalanceBlock.NegativeBalance =>
                "That account balance is below zero, so it is not copied.",
            DebtAccountBalanceBlock.CurrencyDiffers =>
                "That account uses a different currency, so its balance is not copied.",
            DebtAccountBalanceBlock.AmountTooLarge =>
                "That account balance is too large to copy.",
            _ => ""
        };
    }

    /// <summary>
    /// Sums the known amounts. The unknown count is how many were left out.
    /// An empty set of known amounts stays null rather than becoming zero.
    /// </summary>
    private static (decimal? Total, int UnknownCount) SumKnown(IEnumerable<decimal?> values)
    {
        decimal? total = null;
        var unknown = 0;
        foreach (var value in values)
        {
            if (value is decimal amount)
            {
                total = (total ?? 0) + amount;
            }
            else
            {
                unknown++;
            }
        }

        return (total is decimal sum ? AccountLedger.Round(sum) : null, unknown);
    }

    /// <summary>
    /// Share of the known revolving limits in use.
    /// A revolving debt missing a balance or a limit is counted and left out of the ratio.
    /// </summary>
    private static (decimal? Utilization, int Included, int Unknown) CombinedUtilization(
        IReadOnlyList<(DebtSummaryInput Input, DebtSummaryItem Item)> pairs)
    {
        decimal balances = 0;
        decimal limits = 0;
        var included = 0;
        var unknown = 0;
        foreach (var (input, _) in pairs)
        {
            if (input.Kind != DebtKind.Revolving)
            {
                continue;
            }

            if (DebtRules.Utilization(input.Balance, input.CreditLimit) is null
                || input.Balance is not decimal balance
                || input.CreditLimit is not decimal limit)
            {
                unknown++;
                continue;
            }

            balances += balance;
            limits += limit;
            included++;
        }

        if (included == 0)
        {
            return (null, 0, unknown);
        }

        return (
            decimal.Round(balances / limits, 4, MidpointRounding.AwayFromZero),
            included,
            unknown);
    }

    /// <summary>
    /// Counts debts that are still missing one term.
    /// </summary>
    private static int GapCount(IReadOnlyList<DebtSummaryItem> items, DebtSummaryGap gap)
    {
        return items.Count(item => item.Gaps.Contains(gap));
    }

    /// <summary>
    /// Trims a currency code. Blank stays null so it cannot match by accident.
    /// </summary>
    private static string? NormalizeCurrency(string? currency)
    {
        var trimmed = currency?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    #endregion
}
