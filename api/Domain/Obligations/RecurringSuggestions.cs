using System.Text;
using Cardui.Api.Domain.Categories;
using Cardui.Api.Domain.Transactions;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.Obligations;

public static class RecurringSuggestions
{
    public const int HistoryYears = 3;

    /// <summary>
    /// Finds payment patterns that look like bills and are not bills yet.
    /// A pattern needs three payments in the last three years, amounts near one typical payment,
    /// and a gap that matches a schedule. One missed gap is not suggested.
    /// The latest payment has to be recent for that schedule. Transfers, income, pending rows,
    /// archived rows, reconciliations, and other currencies are ignored.
    /// A bill name, a stored suggestion key, or a dismissal hides the pattern.
    /// The amount is one payment, not a monthly total.
    /// </summary>
    public static IReadOnlyList<RecurringSuggestion> Find(
        IEnumerable<RecurringSuggestionCharge> charges,
        IEnumerable<string> billNames,
        IEnumerable<string?> billKeys,
        IEnumerable<string> dismissedKeys,
        string planningCurrency,
        DateOnly today)
    {
        var blocked = BlockedKeys(billNames, billKeys, dismissedKeys);
        var historyStart = today.AddYears(-HistoryYears);
        var suggestions = new List<RecurringSuggestion>();

        foreach (var group in charges.Where(charge => IsEligible(charge, planningCurrency, historyStart, today))
                     .GroupBy(KeyFor))
        {
            if (group.Key is null || blocked.Contains(group.Key))
            {
                continue;
            }

            var suggestion = TryBuild(group.Key, group.ToList(), today);
            if (suggestion is not null)
            {
                suggestions.Add(suggestion);
            }
        }

        return suggestions
            .OrderBy(suggestion => suggestion.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(suggestion => suggestion.Key, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Collapses whitespace and lowercases a merchant pattern key.
    /// An empty key or a key longer than the stored maximum is rejected.
    /// </summary>
    public static bool TryNormalizeKey(string? value, out string key, out string error)
    {
        var collapsed = Collapse(value);
        key = collapsed.ToLowerInvariant();
        if (collapsed.Length == 0 || key.Length > ObligationSuggestionDismissal.KeyMaxLength)
        {
            key = "";
            error = "That suggestion was not found.";
            return false;
        }

        error = "";
        return true;
    }

    #region Private Methods

    /// <summary>
    /// Builds the set of pattern keys that must not be suggested.
    /// Names and stored keys are normalized the same way as a suggestion key.
    /// </summary>
    private static HashSet<string> BlockedKeys(
        IEnumerable<string> billNames,
        IEnumerable<string?> billKeys,
        IEnumerable<string> dismissedKeys)
    {
        var blocked = new HashSet<string>(StringComparer.Ordinal);
        AddKeys(blocked, billNames);
        AddKeys(blocked, billKeys);
        AddKeys(blocked, dismissedKeys);
        return blocked;
    }

    /// <summary>
    /// Adds each value that normalizes to a pattern key.
    /// A blank value is skipped.
    /// </summary>
    private static void AddKeys(HashSet<string> blocked, IEnumerable<string?> values)
    {
        foreach (var value in values)
        {
            if (TryNormalizeKey(value, out var key, out _))
            {
                blocked.Add(key);
            }
        }
    }

    /// <summary>
    /// True when the charge can count toward a suggested bill.
    /// Spending in the planning currency is kept. Income, transfers, and reconciliations are not bills.
    /// </summary>
    private static bool IsEligible(
        RecurringSuggestionCharge charge,
        string planningCurrency,
        DateOnly historyStart,
        DateOnly today)
    {
        return !charge.Pending
            && !charge.Archived
            && charge.Provenance != FinancialRecordProvenance.BalanceReconciliation
            && charge.Amount > 0
            && charge.Date >= historyStart
            && charge.Date <= today
            && PlanningCurrencyRules.IsIncluded(charge.Currency, planningCurrency)
            && !IsTransferOrIncome(charge)
            && KeyFor(charge) is not null;
    }

    /// <summary>
    /// True when the category or its group is a transfer or income.
    /// Those rows are not household bills.
    /// </summary>
    private static bool IsTransferOrIncome(RecurringSuggestionCharge charge)
    {
        return HasKey(charge.GroupKey, SystemGroupKeys.Transfers)
            || HasKey(charge.CategoryKey, SystemCategoryKeys.Transfers)
            || HasKey(charge.GroupKey, SystemGroupKeys.Income)
            || HasKey(charge.CategoryKey, SystemCategoryKeys.Income);
    }

    /// <summary>
    /// Compares a category or group key without regard to case.
    /// </summary>
    private static bool HasKey(string? actual, string expected)
    {
        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The pattern key for a charge, or null when the merchant text is blank or too long.
    /// Merchant name wins over the transaction name.
    /// </summary>
    private static string? KeyFor(RecurringSuggestionCharge charge)
    {
        var display = MerchantMatchKey.Create(charge.Name, charge.MerchantName).DisplayName;
        return TryNormalizeKey(display, out var key, out _) ? key : null;
    }

    /// <summary>
    /// Turns one merchant's charges into a suggestion when the amount and the gap are steady.
    /// Same-day charges count as one payment, using the larger amount.
    /// Returns null when the pattern is too short, uneven, or no longer recent.
    /// </summary>
    private static RecurringSuggestion? TryBuild(
        string key,
        List<RecurringSuggestionCharge> charges,
        DateOnly today)
    {
        var events = Events(charges);
        if (events.Count < 3)
        {
            return null;
        }

        var dates = events.Select(item => item.Date).ToList();
        if (!TryDetectCadence(dates, out var cadence, out var earlyDay, out var lateDay)
            || !IsRecent(cadence, dates[^1], today)
            || !TryTypicalAmount(events.Select(item => item.Amount).ToList(), out var amount))
        {
            return null;
        }

        var name = BillName(events[^1].DisplayName);
        if (name.Length == 0)
        {
            return null;
        }

        var accountId = SharedAccountId(events);
        return new RecurringSuggestion(
            key,
            name,
            amount,
            cadence,
            NextDue(cadence, dates[^1], earlyDay, lateDay),
            accountId,
            accountId is null ? null : events[^1].AccountName);
    }

    /// <summary>
    /// Collapses charges onto one event per day.
    /// The amount is the largest charge that day, so a smaller same-day row does not replace the payment.
    /// </summary>
    private static List<RecurringSuggestionEvent> Events(List<RecurringSuggestionCharge> charges)
    {
        return charges
            .GroupBy(charge => charge.Date)
            .Select(day =>
            {
                var chosen = day.OrderByDescending(charge => charge.Amount).First();
                var display = Collapse(
                    MerchantMatchKey.Create(chosen.Name, chosen.MerchantName).DisplayName);
                return new RecurringSuggestionEvent(
                    day.Key,
                    chosen.Amount,
                    chosen.AccountId,
                    chosen.AccountName,
                    display);
            })
            .OrderBy(item => item.Date)
            .ToList();
    }

    /// <summary>
    /// Reads a schedule from the gaps between payment dates.
    /// A steady 13 to 15 day gap is every two weeks. A gap that stretches toward the next month,
    /// with two days of the month, is twice a month.
    /// </summary>
    private static bool TryDetectCadence(
        IReadOnlyList<DateOnly> dates,
        out ObligationCadence cadence,
        out int earlyDay,
        out int lateDay)
    {
        cadence = default;
        earlyDay = 0;
        lateDay = 0;
        var gaps = new List<int>(dates.Count - 1);
        for (var index = 1; index < dates.Count; index++)
        {
            gaps.Add(dates[index].DayNumber - dates[index - 1].DayNumber);
        }

        if (GapsFit(gaps, 6, 8))
        {
            cadence = ObligationCadence.Weekly;
            return true;
        }

        if (GapsFit(gaps, 13, 15))
        {
            cadence = ObligationCadence.Biweekly;
            return true;
        }

        if (GapsFit(gaps, 12, 18) && TryDayClusters(dates, out earlyDay, out lateDay))
        {
            cadence = ObligationCadence.Semimonthly;
            return true;
        }

        if (GapsFit(gaps, 27, 33))
        {
            cadence = ObligationCadence.Monthly;
            return true;
        }

        if (GapsFit(gaps, 85, 95))
        {
            cadence = ObligationCadence.Quarterly;
            return true;
        }

        if (GapsFit(gaps, 350, 380))
        {
            cadence = ObligationCadence.Yearly;
            return true;
        }

        return false;
    }

    /// <summary>
    /// True when every gap between payments falls in the inclusive day range.
    /// </summary>
    private static bool GapsFit(IReadOnlyList<int> gaps, int min, int max)
    {
        return gaps.Count > 0 && gaps.All(gap => gap >= min && gap <= max);
    }

    /// <summary>
    /// True when the payment days form two clusters, such as the 1st and the 15th.
    /// The clusters have to be at least ten days apart, and each cluster spans at most two days.
    /// </summary>
    private static bool TryDayClusters(
        IReadOnlyList<DateOnly> dates,
        out int earlyDay,
        out int lateDay)
    {
        earlyDay = 0;
        lateDay = 0;
        var unique = dates.Select(date => date.Day).Distinct().OrderBy(day => day).ToArray();
        if (unique.Length < 2)
        {
            return false;
        }

        var splitAfter = 0;
        var bestGap = 0;
        for (var index = 0; index < unique.Length - 1; index++)
        {
            var gap = unique[index + 1] - unique[index];
            if (gap > bestGap)
            {
                bestGap = gap;
                splitAfter = index;
            }
        }

        if (bestGap < 10)
        {
            return false;
        }

        var early = unique.Take(splitAfter + 1).ToArray();
        var late = unique.Skip(splitAfter + 1).ToArray();
        if (early[^1] - early[0] > 2 || late[^1] - late[0] > 2)
        {
            return false;
        }

        earlyDay = (int)Math.Round(early.Average());
        lateDay = (int)Math.Round(late.Average());
        return lateDay - earlyDay >= 10;
    }

    /// <summary>
    /// True when the latest payment is recent enough that the schedule still looks active.
    /// </summary>
    private static bool IsRecent(ObligationCadence cadence, DateOnly latest, DateOnly today)
    {
        var graceDays = cadence switch
        {
            ObligationCadence.Weekly => 14,
            ObligationCadence.Biweekly => 21,
            ObligationCadence.Semimonthly => 24,
            ObligationCadence.Monthly => 45,
            ObligationCadence.Quarterly => 120,
            ObligationCadence.Yearly => 400,
            _ => 0
        };

        return latest >= today.AddDays(-graceDays);
    }

    /// <summary>
    /// Accepts a typical payment when every amount is within a dollar or ten percent of the median, whichever is larger.
    /// The result is that median in dollars and cents.
    /// </summary>
    private static bool TryTypicalAmount(IReadOnlyList<decimal> amounts, out decimal typical)
    {
        typical = 0;
        var sorted = amounts.OrderBy(amount => amount).ToArray();
        var mid = sorted.Length / 2;
        var median = sorted.Length % 2 == 1
            ? sorted[mid]
            : (sorted[mid - 1] + sorted[mid]) / 2m;
        var tolerance = Math.Max(1m, median * 0.10m);
        if (amounts.Any(amount => Math.Abs(amount - median) > tolerance))
        {
            return false;
        }

        typical = decimal.Round(median, 2, MidpointRounding.AwayFromZero);
        return typical > 0 && typical <= ObligationRules.MaxAmount;
    }

    /// <summary>
    /// The next payment date after the latest charge.
    /// Twice a month uses the other day-of-month cluster. The other schedules step by a fixed gap.
    /// </summary>
    private static DateOnly NextDue(
        ObligationCadence cadence,
        DateOnly latest,
        int earlyDay,
        int lateDay)
    {
        return cadence switch
        {
            ObligationCadence.Weekly => latest.AddDays(7),
            ObligationCadence.Biweekly => latest.AddDays(14),
            ObligationCadence.Semimonthly => NextSemimonthly(latest, earlyDay, lateDay),
            ObligationCadence.Monthly => latest.AddMonths(1),
            ObligationCadence.Quarterly => latest.AddMonths(3),
            ObligationCadence.Yearly => latest.AddYears(1),
            _ => latest
        };
    }

    /// <summary>
    /// The other twice-a-month day after the latest payment.
    /// A payment on the early day is followed by the later day in that month. A payment on the later day is followed by the early day next month.
    /// </summary>
    private static DateOnly NextSemimonthly(DateOnly latest, int earlyDay, int lateDay)
    {
        var closerToEarly = Math.Abs(latest.Day - earlyDay) <= Math.Abs(latest.Day - lateDay);
        if (closerToEarly)
        {
            return DateInMonth(latest.Year, latest.Month, lateDay);
        }

        var nextMonth = latest.AddMonths(1);
        return DateInMonth(nextMonth.Year, nextMonth.Month, earlyDay);
    }

    /// <summary>
    /// A calendar day in a month, clamped to the last day when that month is shorter.
    /// </summary>
    private static DateOnly DateInMonth(int year, int month, int day)
    {
        var days = DateTime.DaysInMonth(year, month);
        return new DateOnly(year, month, Math.Min(day, days));
    }

    /// <summary>
    /// The account shared by every payment, or null when the charges left more than one account.
    /// </summary>
    private static Guid? SharedAccountId(IReadOnlyList<RecurringSuggestionEvent> events)
    {
        var accountId = events[0].AccountId;
        return events.All(item => item.AccountId == accountId) ? accountId : null;
    }

    /// <summary>
    /// The bill name shown for a suggestion, limited to the stored bill name length.
    /// </summary>
    private static string BillName(string collapsed)
    {
        if (collapsed.Length <= Obligation.NameMaxLength)
        {
            return collapsed;
        }

        return collapsed[..Obligation.NameMaxLength].TrimEnd();
    }

    /// <summary>
    /// Trims a merchant label and collapses inner whitespace to single spaces.
    /// </summary>
    private static string Collapse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var builder = new StringBuilder(value.Length);
        var pendingSpace = false;
        foreach (var character in value.Trim())
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    #endregion
}
