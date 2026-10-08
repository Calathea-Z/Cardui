using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Plan;

namespace Cardui.Api.Mapping;

public static class PlanCashOutlookDtoMapper
{
    /// <summary>
    /// Copies the cash outlook into the shape the Plan page renders.
    /// Each day keeps its ending cash and the day's income, bills, and debt payments as totals, not the individual rows.
    /// The starting reserve is the amount already set aside. The written assumptions are not sent; the page writes its own copy.
    /// </summary>
    public static PlanCashOutlookDto Map(HouseholdCashOutlookReport report)
    {
        return new PlanCashOutlookDto
        {
            AsOf = report.AsOf,
            StartingCash = report.StartingCash,
            StartingReserve = report.StartingReserve,
            StartingAvailable = report.StartingAvailable,
            HasIncome = report.HasIncome,
            HasBills = report.HasBills,
            ExcludedCurrencies = report.ExcludedCurrencies,
            Rollover = MapPath(report.Rollover),
            ReclaimAll = MapPath(report.ReclaimAll)
        };
    }

    #region Private Methods

    /// <summary>
    /// Copies both income bases for one path. A missing low-pay forecast stays null.
    /// </summary>
    private static PlanCashOutlookPathDto MapPath(HouseholdCashOutlookPath path)
    {
        return new PlanCashOutlookPathDto
        {
            Typical = MapForecast(path.Typical),
            LowPay = path.LowPay is null ? null : MapForecast(path.LowPay)
        };
    }

    /// <summary>
    /// Copies one forecast: the 30 days, the 30-day window, the horizons, and the first shortfall and recovery.
    /// </summary>
    private static PlanCashForecastDto MapForecast(CashForecastReport forecast)
    {
        return new PlanCashForecastDto
        {
            Days = forecast.Days.Select(MapDay).ToList(),
            DayView = MapWindow(forecast.DayView),
            Horizons = forecast.Horizons.Select(MapHorizon).ToList(),
            ShortfallOn = MilestoneDate(forecast, ForecastMilestoneKind.CashShortfall),
            RecoveredOn = MilestoneDate(forecast, ForecastMilestoneKind.CashRecovered),
            ReserveShortfallOn = MilestoneDate(forecast, ForecastMilestoneKind.ReserveShortfall),
            ReserveRestoredOn = MilestoneDate(forecast, ForecastMilestoneKind.ReserveRestored)
        };
    }

    /// <summary>
    /// Copies one day with its income, bills, and debt payments summed by kind.
    /// </summary>
    private static PlanCashDayDto MapDay(CashDay day)
    {
        return new PlanCashDayDto
        {
            Date = day.Date,
            Cash = day.Cash,
            Income = Sum(day, CashFlowKind.Income),
            Bills = Sum(day, CashFlowKind.Bill),
            DebtPayments = Sum(day, CashFlowKind.DebtPayment),
            EverydaySpending = Sum(day, CashFlowKind.EverydaySpending)
        };
    }

    /// <summary>
    /// The total of one kind of event on one day.
    /// </summary>
    private static decimal Sum(CashDay day, CashFlowKind kind)
    {
        return day.Events.Where(item => item.Kind == kind).Sum(item => item.Amount);
    }

    /// <summary>
    /// Copies one stretch of days without the reserve.
    /// </summary>
    private static PlanCashWindowDto MapWindow(CashWindow window)
    {
        return new PlanCashWindowDto
        {
            From = window.From,
            Through = window.Through,
            EndingCash = window.EndingCash,
            LowestCash = window.LowestCash,
            LowestCashOn = window.LowestCashOn,
            CashShortfall = window.CashShortfall
        };
    }

    /// <summary>
    /// Copies one horizon with the minimums still due. The debts behind that sum are not sent.
    /// </summary>
    private static PlanCashHorizonDto MapHorizon(CashHorizon horizon)
    {
        return new PlanCashHorizonDto
        {
            Months = horizon.Months,
            Window = MapWindow(horizon.Window),
            MinimumObligation = horizon.MinimumObligation,
            UnknownMinimumCount = horizon.UnknownMinimumCount
        };
    }

    /// <summary>
    /// The date of the first milestone of one kind. Null when there is none.
    /// </summary>
    private static DateOnly? MilestoneDate(CashForecastReport forecast, ForecastMilestoneKind kind)
    {
        return forecast.Milestones.FirstOrDefault(item => item.Kind == kind)?.Date;
    }

    #endregion
}
