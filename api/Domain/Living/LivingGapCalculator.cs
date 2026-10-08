using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Recovery;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.Living;

public static class LivingGapCalculator
{
    /// <summary>
    /// Compares shared monthly pay with bills, minimums, and monthly living spending.
    /// Irregular bills, unknown minimums, and amounts in another currency are named and left out of the totals.
    /// A known zero counts as zero. The result is a monthly average, not cash on a date.
    /// </summary>
    public static LivingGap Measure(
        decimal sharedMonthly,
        IReadOnlyList<DatedBill> bills,
        IReadOnlyList<LivingDebtMinimum> minimums,
        IReadOnlyList<LivingSpendingAmount> livingSpending,
        string planningCurrency)
    {
        var leftOut = new List<string>();
        var billsMonthly = Bills(bills, planningCurrency, leftOut);
        var minimumsMonthly = Minimums(minimums, planningCurrency, leftOut);
        var livingSpendingMonthly = LivingSpending(livingSpending, planningCurrency, leftOut);
        var need = AccountLedger.Round(billsMonthly + minimumsMonthly + livingSpendingMonthly);
        var shared = AccountLedger.Round(sharedMonthly);
        var shortfall = need > shared ? AccountLedger.Round(need - shared) : 0m;
        return new LivingGap(shared, billsMonthly, minimumsMonthly, livingSpendingMonthly, shortfall, leftOut);
    }

    #region Private Methods

    /// <summary>
    /// Adds scheduled bills in the planning currency.
    /// Irregular and another currency are named instead.
    /// </summary>
    private static decimal Bills(
        IReadOnlyList<DatedBill> bills,
        string planningCurrency,
        List<string> leftOut)
    {
        var total = 0m;
        foreach (var bill in bills)
        {
            if (!PlanningCurrencyRules.IsIncluded(bill.Currency, planningCurrency))
            {
                leftOut.Add($"{bill.Name} is in {bill.Currency}.");
                continue;
            }

            var monthly = Monthly(bill.Amount, bill.Cadence);
            if (monthly is null)
            {
                leftOut.Add($"{bill.Name} has no monthly schedule.");
                continue;
            }

            total += monthly.Value;
        }

        return AccountLedger.Round(total);
    }

    /// <summary>
    /// Adds known minimums for positive balances in the planning currency.
    /// A missing balance or minimum is named. A known zero balance has no active payment.
    /// </summary>
    private static decimal Minimums(
        IReadOnlyList<LivingDebtMinimum> minimums,
        string planningCurrency,
        List<string> leftOut)
    {
        var total = 0m;
        foreach (var minimum in minimums)
        {
            if (!PlanningCurrencyRules.IsIncluded(minimum.Currency, planningCurrency))
            {
                leftOut.Add($"{minimum.Name} is in {minimum.Currency}.");
                continue;
            }

            if (minimum.Balance is null)
            {
                leftOut.Add($"{minimum.Name} has no balance yet.");
                continue;
            }

            if (minimum.Balance <= 0)
            {
                continue;
            }

            if (minimum.Amount is not decimal amount)
            {
                leftOut.Add($"{minimum.Name} has no minimum yet.");
                continue;
            }

            total += amount;
        }

        return AccountLedger.Round(total);
    }

    /// <summary>
    /// Adds kept amounts in the planning currency.
    /// Another currency is named.
    /// </summary>
    private static decimal LivingSpending(
        IReadOnlyList<LivingSpendingAmount> amounts,
        string planningCurrency,
        List<string> leftOut)
    {
        var total = 0m;
        foreach (var amount in amounts)
        {
            if (!PlanningCurrencyRules.IsIncluded(amount.Currency, planningCurrency))
            {
                leftOut.Add($"{amount.Name} is in {amount.Currency}.");
                continue;
            }

            if (amount.MonthlyAmount > 0)
            {
                total += amount.MonthlyAmount;
            }
        }

        return AccountLedger.Round(total);
    }

    /// <summary>
    /// The average month for one bill. Irregular has no average.
    /// Weekly and biweekly use a full year, so a month is not two biweekly payments.
    /// </summary>
    private static decimal? Monthly(decimal payment, ObligationCadence cadence)
    {
        var paymentsPerYear = cadence switch
        {
            ObligationCadence.Weekly => 52,
            ObligationCadence.Biweekly => 26,
            ObligationCadence.Semimonthly => 24,
            ObligationCadence.Monthly => 12,
            ObligationCadence.Quarterly => 4,
            ObligationCadence.Yearly => 1,
            _ => 0
        };
        if (paymentsPerYear == 0)
        {
            return null;
        }

        return AccountLedger.Round(payment * paymentsPerYear / 12m);
    }

    #endregion
}
