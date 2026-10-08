using Cardui.Api.Domain.Accounts;

namespace Cardui.Api.Domain.Recovery;

public static class DebtPaymentAllocation
{
    /// <summary>
    /// Pays each debt's minimum, then applies extra in the order given.
    /// Extra left after one debt is offered to the next debt that can take a payment.
    /// A debt that is paid off or missing a date, rate, or minimum is skipped and does not consume extra.
    /// This is one round. It does not choose the order, and it does not roll a freed minimum forward.
    /// Each debt's own ExtraPayment is ignored. The pool is the extra argument.
    /// </summary>
    public static DebtAllocationResult Apply(
        IReadOnlyList<DebtAmortizationInput> debtsInOrder,
        decimal extra)
    {
        var remaining = extra <= 0 ? 0 : AccountLedger.Round(extra);
        var applied = 0m;
        var lines = new List<DebtAllocationLine>(debtsInOrder.Count);
        foreach (var debt in debtsInOrder)
        {
            var line = AllocateOne(debt, remaining);
            lines.Add(line);
            if (line.Period is not DebtPeriod period)
            {
                continue;
            }

            remaining -= period.ExtraPaid;
            applied += period.ExtraPaid;
        }

        return new DebtAllocationResult(lines, applied, remaining);
    }

    #region Private Methods

    /// <summary>
    /// Builds one debt's payment from the extra still available.
    /// A debt that cannot be calculated keeps its skip reason and takes nothing.
    /// </summary>
    private static DebtAllocationLine AllocateOne(DebtAmortizationInput debt, decimal extraLeft)
    {
        var opening = DebtPaymentFacts.Resolve(debt);
        if (!opening.IsResolved)
        {
            return new DebtAllocationLine(debt.DebtId, null, opening.Skip);
        }

        var period = DebtPeriodCalculator.Calculate(
            opening.DueDate,
            opening.Balance,
            opening.RatePercent,
            opening.RateIsPromotional,
            opening.Minimum,
            extraLeft);
        return new DebtAllocationLine(debt.DebtId, period, null);
    }

    #endregion
}
