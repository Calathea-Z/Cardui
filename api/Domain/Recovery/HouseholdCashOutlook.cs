namespace Cardui.Api.Domain.Recovery;

public static class HouseholdCashOutlook
{
    /// <summary>
    /// Forecasts cash on each payoff path, at typical pay and at low pay.
    /// Debt payments follow the path, so rollover keeps a freed minimum in debt payments and keeping freed payments returns it to cash.
    /// Low pay uses each source's low amount where recorded, otherwise typical, and leaves out expected raises, which are typical amounts.
    /// No savings contribution or protected reserve is stored yet, so the reserve stays at zero.
    /// Debts are the ones the comparison was built from. A debt the comparison left out pays nothing.
    /// </summary>
    public static HouseholdCashOutlookReport Project(
        HouseholdCashOutlookInput input,
        IReadOnlyList<PayoffDebt> debts,
        PayoffRolloverComparison comparison)
    {
        var terms = debts.Select(debt => debt.Terms).ToList();
        var typical = Forecast(input, terms, ForecastIncomeBasis.Typical);
        var low = HasLowPay(input) ? Forecast(input, terms, ForecastIncomeBasis.Conservative) : null;
        var rollover = ForPath(comparison.Rollover, typical, low);
        return new HouseholdCashOutlookReport(
            input.AsOf,
            input.StartingCash,
            input.Incomes.Any(income => Included(income.Currency, input.PlanningCurrency)),
            input.Bills.Any(bill => Included(bill.Currency, input.PlanningCurrency)),
            rollover.Typical.ExcludedCurrencies,
            rollover,
            ForPath(comparison.ReclaimAll, typical, low));
    }

    #region Private Methods

    /// <summary>
    /// True when a row counts in the planning currency. A blank currency counts.
    /// </summary>
    private static bool Included(string? currency, string planningCurrency)
    {
        return PlanningCurrencyRules.IsIncluded(currency, planningCurrency);
    }

    /// <summary>
    /// True when a source in the planning currency records a low payment.
    /// Without one, low pay would match typical pay apart from raises, so it is not forecast.
    /// </summary>
    private static bool HasLowPay(HouseholdCashOutlookInput input)
    {
        return input.Incomes.Any(income =>
            income.LowAmount is not null && Included(income.Currency, input.PlanningCurrency));
    }

    /// <summary>
    /// The forecast input for one income basis, before a path's debt payments are applied.
    /// </summary>
    private static CashForecastInput Forecast(
        HouseholdCashOutlookInput input,
        IReadOnlyList<DebtAmortizationInput> debts,
        ForecastIncomeBasis basis)
    {
        return new CashForecastInput(
            input.PlanningCurrency,
            basis,
            input.AsOf,
            input.StartingCash,
            0m,
            input.Incomes.Select(income => Dated(income, basis)).ToList(),
            input.Bills,
            debts,
            []);
    }

    /// <summary>
    /// One income source at the chosen basis.
    /// Conservative uses the low amount when recorded and drops raises. Typical keeps the typical amount and its raises.
    /// </summary>
    private static DatedIncome Dated(HouseholdIncome income, ForecastIncomeBasis basis)
    {
        var conservative = basis == ForecastIncomeBasis.Conservative;
        return new DatedIncome(
            income.Id,
            income.Name,
            income.Currency,
            conservative ? income.LowAmount ?? income.TypicalAmount : income.TypicalAmount,
            income.Cadence,
            income.NextPaymentDate,
            conservative ? [] : income.Raises);
    }

    /// <summary>
    /// Runs both income bases with the debt payments of one path.
    /// </summary>
    private static HouseholdCashOutlookPath ForPath(
        PayoffRolloverPath path,
        CashForecastInput typical,
        CashForecastInput? low)
    {
        return new HouseholdCashOutlookPath(
            path.Kind,
            CashForecast.Project(typical, path.Schedules),
            low is null ? null : CashForecast.Project(low, path.Schedules));
    }

    #endregion
}
