using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Debts;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.Recovery;

public static class RestructuringComparisonRules
{
    /// <summary>
    /// Compares one offer with keeping the selected debts on their current minimums.
    /// Each selected balance is included in full. The offer rate stays fixed for the whole term.
    /// A lower payment is not ranked above a higher total cost. Nothing is saved, and the offer is not treated as approved.
    /// </summary>
    public static bool TryCompare(
        string planningCurrency,
        DateOnly asOf,
        IReadOnlyList<RestructuringDebt> debts,
        RestructuringOffer offer,
        RestructuringCash cash,
        out RestructuringComparison? comparison,
        out string error)
    {
        comparison = null;
        if (!TryReadOffer(offer, out var kind, out var apr, out var termMonths, out var fee, out var feeFunding, out var upfrontCash, out error))
        {
            return false;
        }

        if (!TryReadDebts(planningCurrency, debts, kind, out error))
        {
            return false;
        }

        if (!TryPrincipal(debts, fee, feeFunding, upfrontCash, out var coveredBalance, out var principal, out error))
        {
            return false;
        }

        var payment = DebtMinimum.Resolve(DebtKind.Installment, principal, apr, null, termMonths);
        if (payment is not decimal monthly)
        {
            error = "That APR and term can't be calculated.";
            return false;
        }

        comparison = Build(
            planningCurrency,
            asOf,
            debts,
            cash,
            kind,
            apr,
            termMonths,
            fee,
            feeFunding,
            upfrontCash,
            coveredBalance,
            principal,
            monthly);
        error = "";
        return true;
    }

    #region Private Methods

    /// <summary>
    /// Checks the terms the person entered.
    /// A blank, a negative, or an amount that is not cents is rejected. The rate allows three decimal places.
    /// </summary>
    private static bool TryReadOffer(
        RestructuringOffer offer,
        out RestructuringOfferKind kind,
        out decimal apr,
        out int termMonths,
        out decimal fee,
        out RestructuringFeeFunding feeFunding,
        out decimal upfrontCash,
        out string error)
    {
        kind = default;
        apr = 0;
        termMonths = 0;
        fee = 0;
        feeFunding = default;
        upfrontCash = 0;
        if (offer.Kind is not RestructuringOfferKind kindValue)
        {
            error = "Choose a consolidation loan, a balance transfer, or a replacement loan.";
            return false;
        }

        kind = kindValue;
        if (!TryReadApr(offer.Apr, out apr, out error))
        {
            return false;
        }

        if (offer.TermMonths is not int months || months < 1 || months > DebtRules.MaxRemainingTermMonths)
        {
            error = "Enter a term from 1 to 600 months.";
            return false;
        }

        termMonths = months;
        if (!TryReadMoney(offer.Fee, "Enter the fee in dollars and cents, or zero.", out fee, out error))
        {
            return false;
        }

        if (offer.FeeFunding is not RestructuringFeeFunding funding)
        {
            error = "Choose whether the fee is added to the balance or paid in cash.";
            return false;
        }

        feeFunding = funding;
        return TryReadMoney(
            offer.UpfrontCash,
            "Enter upfront cash in dollars and cents, or zero.",
            out upfrontCash,
            out error);
    }

    /// <summary>
    /// Checks that the selected debts match the offer and can be replaced in full.
    /// A duplicate, another currency, a missing balance, or a paid-off debt is rejected.
    /// </summary>
    private static bool TryReadDebts(
        string planningCurrency,
        IReadOnlyList<RestructuringDebt> debts,
        RestructuringOfferKind kind,
        out string error)
    {
        if (debts.Select(debt => debt.DebtId).Distinct().Count() != debts.Count)
        {
            error = "Each debt can be included once.";
            return false;
        }

        if (!MatchesKind(debts, kind, out error))
        {
            return false;
        }

        foreach (var debt in debts)
        {
            if (!PlanningCurrencyRules.IsIncluded(debt.Currency, planningCurrency))
            {
                error = $"{debt.Name} is in another currency, so it can't be part of this comparison.";
                return false;
            }

            if (debt.Balance is not decimal balance)
            {
                error = $"{debt.Name} has no balance.";
                return false;
            }

            if (balance <= 0)
            {
                error = $"{debt.Name} is already paid off.";
                return false;
            }

            if (balance > DebtRules.MaxAmount)
            {
                error = "That amount is too large.";
                return false;
            }
        }

        error = "";
        return true;
    }

    /// <summary>
    /// Checks the debt count and, for a balance transfer, that every debt revolves.
    /// </summary>
    private static bool MatchesKind(
        IReadOnlyList<RestructuringDebt> debts,
        RestructuringOfferKind kind,
        out string error)
    {
        if (kind == RestructuringOfferKind.ReplacementLoan && debts.Count != 1)
        {
            error = debts.Count == 0
                ? "Select the debt this loan replaces."
                : "A replacement loan replaces one debt. Two or more debts are a consolidation loan.";
            return false;
        }

        if (kind == RestructuringOfferKind.ConsolidationLoan && debts.Count < 2)
        {
            error = "A consolidation loan replaces two or more debts. A single debt is a replacement loan.";
            return false;
        }

        if (kind == RestructuringOfferKind.BalanceTransfer)
        {
            if (debts.Count < 1)
            {
                error = "Select at least one revolving debt.";
                return false;
            }

            var installment = debts.FirstOrDefault(debt => debt.Kind != DebtKind.Revolving);
            if (installment is not null)
            {
                error = $"A balance transfer can include only revolving debts. {installment.Name} is an installment loan.";
                return false;
            }
        }

        error = "";
        return true;
    }

    /// <summary>
    /// The new balance is the selected balances, plus a fee added to the balance, minus upfront cash.
    /// Upfront cash that pays the balances off leaves no loan to compare.
    /// </summary>
    private static bool TryPrincipal(
        IReadOnlyList<RestructuringDebt> debts,
        decimal fee,
        RestructuringFeeFunding feeFunding,
        decimal upfrontCash,
        out decimal coveredBalance,
        out decimal principal,
        out string error)
    {
        coveredBalance = 0;
        principal = 0;
        foreach (var debt in debts)
        {
            coveredBalance += AccountLedger.Round(debt.Balance ?? 0);
        }

        if (coveredBalance > DebtRules.MaxAmount)
        {
            error = "That amount is too large.";
            return false;
        }

        var financed = feeFunding == RestructuringFeeFunding.Financed ? fee : 0;
        if (upfrontCash > coveredBalance + financed)
        {
            error = "Upfront cash can't be more than the balances plus a fee added to the balance.";
            return false;
        }

        principal = coveredBalance + financed - upfrontCash;
        if (principal <= 0)
        {
            error = "Upfront cash pays these balances, so there is no loan or transfer left to compare.";
            return false;
        }

        if (principal > DebtRules.MaxAmount)
        {
            error = "That amount is too large.";
            return false;
        }

        error = "";
        return true;
    }

    /// <summary>
    /// Reads a required percent with up to three decimal places.
    /// </summary>
    private static bool TryReadApr(decimal? value, out decimal apr, out string error)
    {
        apr = 0;
        if (value is not decimal rate || rate < 0)
        {
            error = "Enter the APR as a percent.";
            return false;
        }

        if (rate > DebtRules.MaxApr)
        {
            error = "That APR is too large.";
            return false;
        }

        if (decimal.Round(rate, 3, MidpointRounding.AwayFromZero) != rate)
        {
            error = "Enter the APR with up to three decimal places.";
            return false;
        }

        apr = rate;
        error = "";
        return true;
    }

    /// <summary>
    /// Reads a required dollar amount. Zero is allowed. More than two decimal places is rejected.
    /// </summary>
    private static bool TryReadMoney(decimal? value, string invalidMessage, out decimal amount, out string error)
    {
        amount = 0;
        if (value is not decimal money || money < 0)
        {
            error = invalidMessage;
            return false;
        }

        if (money > DebtRules.MaxAmount)
        {
            error = "That amount is too large.";
            return false;
        }

        if (decimal.Round(money, 2, MidpointRounding.AwayFromZero) != money)
        {
            error = invalidMessage;
            return false;
        }

        amount = money;
        error = "";
        return true;
    }

    /// <summary>
    /// Projects the current debts and the offer, then assembles the comparison.
    /// Current debts keep their own minimums. The offer pays the level payment for its term. Neither receives extra.
    /// </summary>
    private static RestructuringComparison Build(
        string planningCurrency,
        DateOnly asOf,
        IReadOnlyList<RestructuringDebt> debts,
        RestructuringCash cash,
        RestructuringOfferKind kind,
        decimal apr,
        int termMonths,
        decimal fee,
        RestructuringFeeFunding feeFunding,
        decimal upfrontCash,
        decimal coveredBalance,
        decimal principal,
        decimal monthly)
    {
        var inputs = debts.Select(ToInput).ToList();
        var schedules = inputs
            .Select(input => DebtAmortization.Project(input, from: asOf))
            .ToList();
        var offerDue = OfferDueDate(debts, asOf);
        var offerSchedule = DebtAmortization.Project(
            OfferInput(planningCurrency, principal, apr, termMonths, monthly, offerDue),
            from: asOf);
        var outcomes = debts
            .Select((debt, index) => ToOutcome(debt, schedules[index]))
            .ToList();
        var keepInterest = TotalInterest(schedules);
        var offerInterest = offerSchedule.Stop == DebtScheduleStop.PaidOff
            ? offerSchedule.Periods.Sum(period => period.Interest)
            : (decimal?)null;
        var keepMonthly = KeepMonthly(inputs);
        var cashDue = (feeFunding == RestructuringFeeFunding.Cash ? fee : 0) + upfrontCash;
        var (breakEven, breakEvenOn) = BreakEven(schedules, offerSchedule, fee);
        return new RestructuringComparison(
            kind,
            outcomes,
            coveredBalance,
            fee,
            feeFunding,
            upfrontCash,
            cashDue,
            principal,
            apr,
            termMonths,
            offerSchedule.Stop == DebtScheduleStop.PaidOff ? offerSchedule.Periods.Count : null,
            monthly,
            keepMonthly,
            keepMonthly is decimal keep ? keep - monthly : null,
            keepInterest,
            offerInterest,
            keepInterest,
            offerInterest is decimal interest ? interest + fee : null,
            LatestPayoff(outcomes),
            offerSchedule.Stop == DebtScheduleStop.PaidOff && offerSchedule.Periods.Count > 0
                ? offerSchedule.Periods[^1].DueDate
                : null,
            offerSchedule.Stop,
            breakEven,
            breakEvenOn,
            CashEffect(cash, cashDue));
    }

    /// <summary>
    /// Copies one debt into the amortization input. Its extra stays zero.
    /// </summary>
    private static DebtAmortizationInput ToInput(RestructuringDebt debt)
    {
        return new DebtAmortizationInput(
            debt.DebtId,
            debt.Name,
            debt.Currency,
            debt.Kind,
            debt.Balance ?? 0,
            debt.Apr,
            debt.PromotionalApr,
            debt.PromotionalEndsOn,
            debt.MinimumPayment,
            debt.RemainingTermMonths,
            debt.NextDueDate,
            0m);
    }

    /// <summary>
    /// The offer as one installment debt at the entered rate for the whole term.
    /// </summary>
    private static DebtAmortizationInput OfferInput(
        string planningCurrency,
        decimal principal,
        decimal apr,
        int termMonths,
        decimal monthly,
        DateOnly due)
    {
        return new DebtAmortizationInput(
            Guid.Empty,
            "This offer",
            planningCurrency,
            DebtKind.Installment,
            principal,
            apr,
            null,
            null,
            monthly,
            termMonths,
            due,
            0m);
    }

    /// <summary>
    /// The earliest due date on or after asOf among the selected debts.
    /// When none is known, the offer's first payment is asOf.
    /// </summary>
    private static DateOnly OfferDueDate(IReadOnlyList<RestructuringDebt> debts, DateOnly asOf)
    {
        DateOnly? earliest = null;
        foreach (var debt in debts)
        {
            if (debt.NextDueDate is not DateOnly due)
            {
                continue;
            }

            if (DebtDueDates.FirstIndexOnOrAfter(due, asOf) is not int index
                || !DebtDueDates.TryStep(due, index, out var first))
            {
                continue;
            }

            if (earliest is null || first < earliest)
            {
                earliest = first;
            }
        }

        return earliest ?? asOf;
    }

    /// <summary>
    /// One replaced debt's current projection.
    /// </summary>
    private static RestructuringDebtOutcome ToOutcome(RestructuringDebt debt, DebtSchedule schedule)
    {
        return new RestructuringDebtOutcome(
            debt.DebtId,
            debt.Name,
            debt.Kind,
            AccountLedger.Round(debt.Balance ?? 0),
            debt.Apr,
            debt.PromotionalApr,
            debt.PromotionalEndsOn,
            schedule.Stop,
            schedule.Stop == DebtScheduleStop.PaidOff && schedule.Periods.Count > 0
                ? schedule.Periods[^1].DueDate
                : null);
    }

    /// <summary>
    /// Interest through payoff. Null when any schedule stops short of payoff.
    /// </summary>
    private static decimal? TotalInterest(IReadOnlyList<DebtSchedule> schedules)
    {
        if (schedules.Any(schedule => schedule.Stop != DebtScheduleStop.PaidOff))
        {
            return null;
        }

        return schedules.Sum(schedule => schedule.Periods.Sum(period => period.Interest));
    }

    /// <summary>
    /// The sum of the current minimums. Null when any minimum cannot be resolved.
    /// A final payoff payment may be smaller than this amount.
    /// </summary>
    private static decimal? KeepMonthly(IReadOnlyList<DebtAmortizationInput> inputs)
    {
        decimal total = 0;
        foreach (var input in inputs)
        {
            var resolved = DebtPaymentFacts.Resolve(input);
            if (!resolved.IsResolved)
            {
                return null;
            }

            total += resolved.Minimum;
        }

        return total;
    }

    /// <summary>
    /// The latest payoff among debts that all pay off. Null when any debt does not.
    /// </summary>
    private static DateOnly? LatestPayoff(IReadOnlyList<RestructuringDebtOutcome> outcomes)
    {
        if (outcomes.Any(outcome => outcome.PaidOffOn is null))
        {
            return null;
        }

        return outcomes.Max(outcome => outcome.PaidOffOn);
    }

    /// <summary>
    /// The first date the offer's interest plus fee stays at or below the current interest through both payoffs.
    /// A path that does not pay off leaves the date unknown, because later interest is not invented.
    /// </summary>
    private static (RestructuringBreakEven Kind, DateOnly? On) BreakEven(
        IReadOnlyList<DebtSchedule> keep,
        DebtSchedule offer,
        decimal fee)
    {
        if (offer.Stop != DebtScheduleStop.PaidOff
            || keep.Any(schedule => schedule.Stop != DebtScheduleStop.PaidOff))
        {
            return (RestructuringBreakEven.Unknown, null);
        }

        var dates = new SortedSet<DateOnly>();
        var keepInterestByDate = InterestByDate(keep, dates);
        var offerInterestByDate = InterestByDate([offer], dates);
        if (dates.Count == 0)
        {
            return (RestructuringBreakEven.Unknown, null);
        }

        decimal keepInterest = 0;
        decimal offerInterest = 0;
        DateOnly? candidate = null;
        foreach (var date in dates)
        {
            if (keepInterestByDate.TryGetValue(date, out var keepOnDate))
            {
                keepInterest += keepOnDate;
            }

            if (offerInterestByDate.TryGetValue(date, out var offerOnDate))
            {
                offerInterest += offerOnDate;
            }

            if (keepInterest >= fee + offerInterest)
            {
                candidate ??= date;
            }
            else
            {
                candidate = null;
            }
        }

        return candidate is DateOnly on
            ? (RestructuringBreakEven.Reached, on)
            : (RestructuringBreakEven.Never, null);
    }

    /// <summary>
    /// Interest charged on each due date, and every date added to the shared set.
    /// </summary>
    private static Dictionary<DateOnly, decimal> InterestByDate(
        IReadOnlyList<DebtSchedule> schedules,
        ISet<DateOnly> dates)
    {
        var interest = new Dictionary<DateOnly, decimal>();
        foreach (var period in schedules.SelectMany(schedule => schedule.Periods))
        {
            dates.Add(period.DueDate);
            interest[period.DueDate] = interest.GetValueOrDefault(period.DueDate) + period.Interest;
        }

        return interest;
    }

    /// <summary>
    /// How cash due now sits against cash on hand, the reserve, and cash to keep.
    /// Cash due is a fee paid in cash plus upfront cash. A financed fee is not cash due.
    /// </summary>
    private static RestructuringCashEffect CashEffect(RestructuringCash cash, decimal cashDue)
    {
        var onHand = AccountLedger.Round(cash.CashOnHand);
        var reserve = AccountLedger.Round(cash.Reserve);
        var floor = AccountLedger.Round(cash.CashToKeep);
        return new RestructuringCashEffect(
            onHand,
            reserve,
            floor,
            cash.HasCashFloor,
            cashDue > onHand,
            cashDue > 0 && cashDue > onHand - reserve,
            cash.HasCashFloor && cashDue > 0 && onHand - cashDue < floor);
    }

    #endregion
}
