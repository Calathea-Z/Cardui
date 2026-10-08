namespace Cardui.Api.Domain.Recovery;

public static class HouseholdRecovery
{
    /// <summary>
    /// Builds the rollover input for the household's debts.
    /// Shared extra, a custom order, and a reclaim amount are not stored, so this uses avalanche, no shared extra, and no reclaim.
    /// Each debt's own extra is zero because that amount is not stored either.
    /// A missing balance is left out. Zero would be read as already paid off.
    /// </summary>
    public static PayoffRolloverInput Prepare(
        string planningCurrency,
        IReadOnlyList<HouseholdRecoveryDebt> debts)
    {
        return new PayoffRolloverInput(
            planningCurrency,
            0m,
            Included(debts),
            [],
            0m);
    }

    #region Private Methods

    /// <summary>
    /// Copies each debt that has a balance into the payoff input.
    /// Another currency is kept so the rollover rule can leave it out. A missing rate, minimum, or due date stays null.
    /// </summary>
    private static List<PayoffDebt> Included(IReadOnlyList<HouseholdRecoveryDebt> debts)
    {
        var included = new List<PayoffDebt>();
        foreach (var debt in debts)
        {
            if (debt.BalanceInUse is not decimal balance)
            {
                continue;
            }

            included.Add(new PayoffDebt(
                new DebtAmortizationInput(
                    debt.DebtId,
                    debt.Name,
                    debt.Currency,
                    debt.Kind,
                    balance,
                    debt.Apr,
                    debt.PromotionalApr,
                    debt.PromotionalEndsOn,
                    debt.MinimumPayment,
                    debt.RemainingTermMonths,
                    debt.NextDueDate,
                    0m),
                debt.CreditLimit));
        }

        return included;
    }

    #endregion
}
