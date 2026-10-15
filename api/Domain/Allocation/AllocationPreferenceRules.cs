using Cardui.Api.Domain.Accounts;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.Allocation;

public static class AllocationPreferenceRules
{
    /// <summary>
    /// A variable source in another currency is named and left out of the split.
    /// </summary>
    public const string OtherCurrency = "Another currency";

    /// <summary>
    /// A variable source with no strong payment above typical has nothing to split.
    /// </summary>
    public const string NoStrongSurplus = "No strong amount above typical";

    /// <summary>
    /// Checks both rules. A percent outside 0 to 100 is rejected.
    /// Debt and savings that add past 100 are rejected so the remainder can stay available.
    /// </summary>
    public static bool TryNormalize(
        int variableIncomeDebtPercent,
        int variableIncomeSavingsPercent,
        int windfallDebtPercent,
        int windfallSavingsPercent,
        out AllocationPreferenceDraft? draft,
        out string error)
    {
        if (!TryReadPair(
                variableIncomeDebtPercent,
                variableIncomeSavingsPercent,
                "Variable income",
                out var variableDebt,
                out var variableSavings,
                out error)
            || !TryReadPair(
                windfallDebtPercent,
                windfallSavingsPercent,
                "Windfall",
                out var windfallDebt,
                out var windfallSavings,
                out error))
        {
            draft = null;
            return false;
        }

        draft = new AllocationPreferenceDraft(
            variableDebt,
            variableSavings,
            windfallDebt,
            windfallSavings);
        error = "";
        return true;
    }

    /// <summary>
    /// The share that stays available after debt and savings.
    /// The two stored percents already fit in 0 to 100 together.
    /// </summary>
    public static int CashPercent(int debtPercent, int savingsPercent)
    {
        return 100 - debtPercent - savingsPercent;
    }

    /// <summary>
    /// Splits one positive amount into extra debt, savings, and what stays available.
    /// Cents round away from zero. When rounding would pass the amount, savings is reduced first, then debt.
    /// A zero or negative amount stays all zeros. This does not move money.
    /// </summary>
    public static AllocationSplit Split(decimal amount, int debtPercent, int savingsPercent)
    {
        var rounded = AccountLedger.Round(amount);
        if (rounded <= 0)
        {
            return new AllocationSplit(0m, 0m, 0m);
        }

        var debt = AccountLedger.Round(rounded * debtPercent / 100m);
        var savings = AccountLedger.Round(rounded * savingsPercent / 100m);
        var overflow = debt + savings - rounded;
        if (overflow > 0)
        {
            var fromSavings = overflow > savings ? savings : overflow;
            savings -= fromSavings;
            overflow -= fromSavings;
            if (overflow > 0)
            {
                debt -= overflow > debt ? debt : overflow;
            }
        }

        return new AllocationSplit(debt, savings, rounded - debt - savings);
    }

    /// <summary>
    /// Lists variable sources for review. Steady and uncertain pay are skipped.
    /// Another currency is left out even when a strong amount is recorded.
    /// A matching-currency source without a strong amount above typical is named and not split.
    /// </summary>
    public static IReadOnlyList<VariableIncomeReview> Review(
        IReadOnlyList<VariableIncomePayment> payments,
        string planningCurrency)
    {
        var reviews = new List<VariableIncomeReview>();
        foreach (var payment in payments)
        {
            if (payment.Reliability != IncomeReliability.Variable)
            {
                continue;
            }

            if (!PlanningCurrencyRules.IsIncluded(payment.Currency, planningCurrency))
            {
                reviews.Add(new VariableIncomeReview(
                    payment.Name,
                    payment.Currency,
                    null,
                    OtherCurrency));
                continue;
            }

            var surplus = Surplus(payment);
            reviews.Add(surplus is null
                ? new VariableIncomeReview(payment.Name, payment.Currency, null, NoStrongSurplus)
                : new VariableIncomeReview(payment.Name, payment.Currency, surplus, null));
        }

        return reviews;
    }

    #region Private Methods

    /// <summary>
    /// Checks one rule's debt and savings percents.
    /// The label names which rule failed.
    /// </summary>
    private static bool TryReadPair(
        int debtPercent,
        int savingsPercent,
        string label,
        out int debt,
        out int savings,
        out string error)
    {
        if (!TryReadPercent(debtPercent, label, "extra debt", out debt, out error)
            || !TryReadPercent(savingsPercent, label, "savings", out savings, out error))
        {
            savings = 0;
            return false;
        }

        if (debt + savings > 100)
        {
            error = $"{label}: debt and savings together can't be more than 100 percent. The rest stays available.";
            return false;
        }

        error = "";
        return true;
    }

    /// <summary>
    /// Checks one whole percent.
    /// Values outside 0 to 100 are rejected.
    /// </summary>
    private static bool TryReadPercent(
        int percent,
        string label,
        string name,
        out int stored,
        out string error)
    {
        if (percent is < 0 or > 100)
        {
            stored = 0;
            error = $"{label}: enter a whole percent from 0 to 100 for {name}.";
            return false;
        }

        stored = percent;
        error = "";
        return true;
    }

    /// <summary>
    /// The amount a strong payment sits above typical pay.
    /// Null when no strong payment is recorded or it is not above typical.
    /// </summary>
    private static decimal? Surplus(VariableIncomePayment payment)
    {
        if (payment.StrongAmount is not decimal strong || strong <= payment.TypicalAmount)
        {
            return null;
        }

        return AccountLedger.Round(strong - payment.TypicalAmount);
    }

    #endregion
}
