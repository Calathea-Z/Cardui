namespace Cardui.Api.Domain.Recovery;

public static class HouseholdRecovery
{
    /// <summary>
    /// Builds the rollover input for the household's debts.
    /// Monthly extra is shared extra tried for this calculation. Zero is minimums only. It is not stored.
    /// A custom order and a reclaim amount are not stored, so this uses avalanche and no reclaim.
    /// Each debt's own extra is zero because that amount is not stored either.
    /// A missing balance is left out of the input, because zero would be read as already paid off. That debt is listed instead.
    /// Payments start on or after asOf, so a stored due date that has already passed is not replayed against today's balance.
    /// </summary>
    public static HouseholdRecoveryInput Prepare(
        string planningCurrency,
        DateOnly asOf,
        IReadOnlyList<HouseholdRecoveryDebt> debts,
        decimal monthlyExtra)
    {
        return new HouseholdRecoveryInput(
            new PayoffRolloverInput(
                planningCurrency,
                monthlyExtra,
                Included(debts),
                [],
                0m,
                asOf),
            MissingBalance(debts));
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

    /// <summary>
    /// The debts with no balance in use, in the order given.
    /// A debt in another currency is listed too, because the currency rule never sees it.
    /// </summary>
    private static List<HouseholdRecoveryMissingBalance> MissingBalance(IReadOnlyList<HouseholdRecoveryDebt> debts)
    {
        return debts
            .Where(debt => debt.BalanceInUse is null)
            .Select(debt => new HouseholdRecoveryMissingBalance(debt.DebtId, debt.Name))
            .ToList();
    }

    #endregion
}
