using System.Globalization;

namespace Cardui.Api.Domain;

public static class MerchantHistoryPeriods
{
    /// <summary>
    /// Fills every period from the earliest transaction through today.
    /// The period that contains today is included even when it has no transactions.
    /// </summary>
    public static MerchantHistorySeries Build(
        DateOnly today,
        string? granularity,
        IEnumerable<MerchantHistoryActivity> transactions)
    {
        var activities = transactions.ToList();
        var normalizedGranularity = NormalizeGranularity(granularity);
        var selectedKey = GetPeriodKey(today, normalizedGranularity);
        var earliestDate = activities.Count == 0
            ? today
            : activities.Min(activity => activity.Date);

        var periods = BuildPeriods(
            earliestDate,
            today,
            normalizedGranularity,
            activities);

        if (periods.All(period => period.Key != selectedKey))
        {
            periods.Add(CreateEmptyPeriod(selectedKey, normalizedGranularity));
            periods = OrderPeriods(periods);
        }

        return new MerchantHistorySeries(
            normalizedGranularity,
            selectedKey,
            periods);
    }

    #region Private Methods

    /// <summary>
    /// Accepts monthly, quarterly, or yearly. Anything else stays monthly.
    /// </summary>
    private static string NormalizeGranularity(string? granularity)
    {
        return granularity?.Trim().ToLowerInvariant() switch
        {
            "quarterly" => "quarterly",
            "yearly" => "yearly",
            _ => "monthly"
        };
    }

    /// <summary>
    /// Builds the period key: yyyy-MM, yyyy-Q#, or yyyy.
    /// </summary>
    private static string GetPeriodKey(DateOnly date, string granularity)
    {
        return granularity switch
        {
            "quarterly" => $"{date.Year}-Q{(date.Month - 1) / 3 + 1}",
            "yearly" => date.Year.ToString(),
            _ => $"{date.Year:D4}-{date.Month:D2}"
        };
    }

    /// <summary>
    /// Builds the long and short labels for a history period.
    /// </summary>
    private static (string Label, string ShortLabel) GetPeriodLabels(
        string periodKey,
        string granularity)
    {
        if (granularity == "yearly" && int.TryParse(periodKey, out var year))
        {
            return (year.ToString(), year.ToString());
        }

        if (granularity == "quarterly")
        {
            var parts = periodKey.Split("-Q");
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out var quarterYear) &&
                int.TryParse(parts[1], out var quarter))
            {
                return ($"Q{quarter} {quarterYear}", $"Q{quarter}");
            }
        }

        if (DateOnly.TryParseExact(
                $"{periodKey}-01",
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var monthDate))
        {
            return (
                monthDate.ToString("MMMM yyyy"),
                monthDate.ToString("MMM"));
        }

        return (periodKey, periodKey);
    }

    /// <summary>
    /// Creates a period with no transactions so the current period is always present.
    /// </summary>
    private static MerchantHistoryPeriod CreateEmptyPeriod(
        string periodKey,
        string granularity)
    {
        var (label, shortLabel) = GetPeriodLabels(periodKey, granularity);
        return new MerchantHistoryPeriod(
            periodKey,
            label,
            shortLabel,
            TotalAmount: 0,
            TransactionCount: 0);
    }

    /// <summary>
    /// Fills every period from the earliest transaction through today, including gaps.
    /// </summary>
    private static List<MerchantHistoryPeriod> BuildPeriods(
        DateOnly startDate,
        DateOnly endDate,
        string granularity,
        IReadOnlyList<MerchantHistoryActivity> transactions)
    {
        var totals = transactions
            .GroupBy(transaction => GetPeriodKey(transaction.Date, granularity))
            .ToDictionary(
                group => group.Key,
                group => (
                    TotalAmount: group.Sum(transaction => transaction.Amount),
                    TransactionCount: group.Count()));

        var periods = new List<MerchantHistoryPeriod>();
        var cursor = AlignPeriodStart(startDate, granularity);
        var end = AlignPeriodStart(endDate, granularity);

        while (cursor <= end)
        {
            var key = GetPeriodKey(cursor, granularity);
            totals.TryGetValue(key, out var stats);
            var (label, shortLabel) = GetPeriodLabels(key, granularity);

            periods.Add(new MerchantHistoryPeriod(
                key,
                label,
                shortLabel,
                stats.TotalAmount,
                stats.TransactionCount));

            cursor = AdvancePeriod(cursor, granularity);
        }

        return periods;
    }

    /// <summary>
    /// Sorts periods by key and drops a duplicate key.
    /// </summary>
    private static List<MerchantHistoryPeriod> OrderPeriods(
        List<MerchantHistoryPeriod> periods)
    {
        return periods
            .DistinctBy(period => period.Key)
            .OrderBy(period => period.Key, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Moves a date to the first day of its month, quarter, or year.
    /// </summary>
    private static DateOnly AlignPeriodStart(DateOnly date, string granularity)
    {
        return granularity switch
        {
            "quarterly" => new DateOnly(date.Year, (date.Month - 1) / 3 * 3 + 1, 1),
            "yearly" => new DateOnly(date.Year, 1, 1),
            _ => new DateOnly(date.Year, date.Month, 1)
        };
    }

    /// <summary>
    /// Steps one month, quarter, or year forward.
    /// </summary>
    private static DateOnly AdvancePeriod(DateOnly date, string granularity)
    {
        return granularity switch
        {
            "quarterly" => date.AddMonths(3),
            "yearly" => date.AddYears(1),
            _ => date.AddMonths(1)
        };
    }

    #endregion
}
