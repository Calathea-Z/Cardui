using Cardui.Api.Domain.Planning;
using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Plan;
using Cardui.Api.Dtos.Planning;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;

namespace Cardui.Api.Services.Implementations;

public class PlanningForecasts : IPlanningForecasts
{
    private readonly IPlanService _planService;
    private readonly HouseholdScope _householdScope;

    public PlanningForecasts(IPlanService planService, HouseholdScope householdScope)
    {
        _planService = planService;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<PlanningForecastDto> DescribeAsync(
        PlanScenarioQuery scenario,
        CancellationToken cancellationToken = default)
    {
        RequireHousehold();
        var recovery = await _planService.GetRecoveryAsync(scenario, cancellationToken);
        var forecast = PlanningForecastRules.Compose(MapFigures(recovery), MapContext(recovery));
        return ToDto(forecast);
    }

    #region Private Methods

    /// <summary>
    /// Requires a household before the calculation is read.
    /// </summary>
    private void RequireHousehold()
    {
        _householdScope.RequireHouseholdId();
    }

    /// <summary>
    /// Copies the calculation's numbers. A null amount is omitted, so a missing minimum is not stored as zero.
    /// </summary>
    private static List<PlanningForecastFigure> MapFigures(PlanRecoveryDto recovery)
    {
        var figures = new List<PlanningForecastFigure>();
        AddAmount(figures, PlanningForecastFigureKind.StartingCash, recovery.CashOutlook.StartingCash);
        AddAmount(figures, PlanningForecastFigureKind.StartingReserve, recovery.CashOutlook.StartingReserve);
        AddAmount(figures, PlanningForecastFigureKind.StartingAvailable, recovery.CashOutlook.StartingAvailable);
        AddAmount(figures, PlanningForecastFigureKind.MonthlyExtra, recovery.MonthlyExtra);
        AddAmount(figures, PlanningForecastFigureKind.ReclaimAmount, recovery.ReclaimAmount);
        AddAmount(figures, PlanningForecastFigureKind.LivingSpending, recovery.LivingSpendingMonthly);
        AddAmount(figures, PlanningForecastFigureKind.SavedCashFloor, recovery.CashFloor);
        if (recovery.IncomeLossMonths > 0)
        {
            AddCount(figures, PlanningForecastFigureKind.IncomeLossMonths, recovery.IncomeLossMonths);
        }

        if (recovery.WindfallAmount > 0)
        {
            AddAmount(figures, PlanningForecastFigureKind.WindfallAmount, recovery.WindfallAmount);
        }

        if (recovery.WindfallOn is DateOnly windfallOn)
        {
            AddDate(figures, PlanningForecastFigureKind.WindfallOn, windfallOn);
        }

        if (recovery.ScenarioLivingSpendingMonthly is decimal spending)
        {
            AddAmount(figures, PlanningForecastFigureKind.FlexibleSpending, spending);
        }

        if (recovery.ScenarioCashFloor is decimal floor)
        {
            AddAmount(figures, PlanningForecastFigureKind.CashToKeep, floor);
        }

        AddPath(figures, recovery.Rollover);
        AddPath(figures, recovery.Reclaim);
        AddPath(figures, recovery.ReclaimAll);
        AddCash(figures, PayoffRolloverKind.Rollover, recovery.CashOutlook.Rollover);
        AddCash(figures, PayoffRolloverKind.Reclaim, recovery.CashOutlook.Reclaim);
        AddCash(figures, PayoffRolloverKind.ReclaimAll, recovery.CashOutlook.ReclaimAll);
        AddSpendable(figures, PayoffRolloverKind.Rollover, recovery.Spendable.Rollover);
        AddSpendable(figures, PayoffRolloverKind.Reclaim, recovery.Spendable.Reclaim);
        AddSpendable(figures, PayoffRolloverKind.ReclaimAll, recovery.Spendable.ReclaimAll);
        foreach (var alternative in recovery.PriorityAlternatives)
        {
            AddAmount(
                figures,
                PlanningForecastFigureKind.InterestDifference,
                alternative.InterestDifference,
                priority: alternative.Choice);
            AddAmount(
                figures,
                PlanningForecastFigureKind.TradeoffCash,
                alternative.TradeoffCash,
                priority: alternative.Choice);
        }

        return figures;
    }

    /// <summary>
    /// Copies one freed-cash path, including each debt, step, and balance the calculation returned.
    /// </summary>
    private static void AddPath(List<PlanningForecastFigure> figures, PlanRecoveryPathDto path)
    {
        AddAmount(figures, PlanningForecastFigureKind.TotalInterest, path.TotalInterest, path.Kind);
        AddAmount(figures, PlanningForecastFigureKind.RecurringRoom, path.RecurringRoom, path.Kind);
        AddAmount(figures, PlanningForecastFigureKind.CashReclaimed, path.CashReclaimed, path.Kind);
        AddAmount(figures, PlanningForecastFigureKind.CashRolled, path.CashRolled, path.Kind);
        AddOptionalAmount(
            figures,
            PlanningForecastFigureKind.StartingObligation,
            path.StartingObligation,
            path.Kind);
        AddOptionalAmount(
            figures,
            PlanningForecastFigureKind.RemainingObligation,
            path.RemainingObligation,
            path.Kind);
        if (path.PaidOffOn is DateOnly paid)
        {
            AddDate(figures, PlanningForecastFigureKind.PaidOffOn, paid, path.Kind);
        }

        foreach (var debt in path.Debts)
        {
            AddDebt(figures, path.Kind, debt);
        }

        foreach (var step in path.Steps)
        {
            AddStep(figures, path.Kind, step);
        }

        foreach (var point in path.BalancePoints)
        {
            var name = path.Debts.FirstOrDefault(debt => debt.DebtId == point.DebtId)?.Name;
            AddAmount(
                figures,
                PlanningForecastFigureKind.BalanceAfterPayment,
                point.Balance,
                path.Kind,
                on: point.DueDate,
                name: name);
        }
    }

    /// <summary>
    /// Copies one debt's calculated outcome. A missing minimum, date, or modeled month is omitted.
    /// </summary>
    private static void AddDebt(
        List<PlanningForecastFigure> figures,
        PayoffRolloverKind path,
        PlanDebtOutcomeDto debt)
    {
        AddAmount(
            figures,
            PlanningForecastFigureKind.DebtOpeningBalance,
            debt.Balance,
            path,
            name: debt.Name);
        AddOptionalAmount(
            figures,
            PlanningForecastFigureKind.DebtMinimum,
            debt.Minimum,
            path,
            name: debt.Name);
        if (debt.PaidOffOn is DateOnly paid)
        {
            AddDate(figures, PlanningForecastFigureKind.DebtPaidOffOn, paid, path, debt.Name);
        }

        AddOptionalAmount(
            figures,
            PlanningForecastFigureKind.DebtLastInterest,
            debt.LastMonthInterest,
            path,
            name: debt.Name);
        AddOptionalAmount(
            figures,
            PlanningForecastFigureKind.DebtLastPayment,
            debt.LastMonthPayment,
            path,
            name: debt.Name);
    }

    /// <summary>
    /// Copies one payoff step. A removal date outside the projection stays omitted.
    /// </summary>
    private static void AddStep(
        List<PlanningForecastFigure> figures,
        PayoffRolloverKind path,
        PlanRecoveryStepDto step)
    {
        AddDate(figures, PlanningForecastFigureKind.StepEndedOn, step.EndedOn, path, step.Name);
        if (step.StartsOn is DateOnly starts)
        {
            AddDate(figures, PlanningForecastFigureKind.StepStartsOn, starts, path, step.Name);
        }

        AddAmount(figures, PlanningForecastFigureKind.StepMinimum, step.Minimum, path, name: step.Name);
        AddAmount(
            figures,
            PlanningForecastFigureKind.StepBreathingRoom,
            step.BreathingRoom,
            path,
            name: step.Name);
    }

    /// <summary>
    /// Copies typical pay, then low pay when the calculation returned it.
    /// </summary>
    private static void AddCash(
        List<PlanningForecastFigure> figures,
        PayoffRolloverKind path,
        PlanCashOutlookPathDto outlook)
    {
        AddForecast(figures, path, PlanningForecastBasis.Typical, outlook.Typical);
        if (outlook.LowPay is PlanCashForecastDto lowPay)
        {
            AddForecast(figures, path, PlanningForecastBasis.LowPay, lowPay);
        }
    }

    /// <summary>
    /// Copies one cash forecast: its windows, its horizon gaps, and each day's amounts.
    /// </summary>
    private static void AddForecast(
        List<PlanningForecastFigure> figures,
        PayoffRolloverKind path,
        PlanningForecastBasis basis,
        PlanCashForecastDto forecast)
    {
        AddWindow(figures, path, basis, 0, forecast.DayView);
        foreach (var horizon in forecast.Horizons)
        {
            AddWindow(figures, path, basis, horizon.Months, horizon.Window);
            AddOptionalAmount(
                figures,
                PlanningForecastFigureKind.MinimumObligation,
                horizon.MinimumObligation,
                path,
                basis,
                horizon.Months);
            if (horizon.UnknownMinimumCount > 0)
            {
                AddCount(
                    figures,
                    PlanningForecastFigureKind.UnknownMinimumCount,
                    horizon.UnknownMinimumCount,
                    path,
                    basis,
                    horizon.Months);
            }
        }

        AddOptionalDate(figures, PlanningForecastFigureKind.ShortfallOn, forecast.ShortfallOn, path, basis);
        AddOptionalDate(figures, PlanningForecastFigureKind.RecoveredOn, forecast.RecoveredOn, path, basis);
        AddOptionalDate(
            figures,
            PlanningForecastFigureKind.ReserveShortfallOn,
            forecast.ReserveShortfallOn,
            path,
            basis);
        AddOptionalDate(
            figures,
            PlanningForecastFigureKind.ReserveRestoredOn,
            forecast.ReserveRestoredOn,
            path,
            basis);
        foreach (var day in forecast.Days)
        {
            AddAmount(figures, PlanningForecastFigureKind.DayCash, day.Cash, path, basis, on: day.Date);
            AddAmount(figures, PlanningForecastFigureKind.DayIncome, day.Income, path, basis, on: day.Date);
            AddAmount(figures, PlanningForecastFigureKind.DayBills, day.Bills, path, basis, on: day.Date);
            AddAmount(
                figures,
                PlanningForecastFigureKind.DayDebtPayment,
                day.DebtPayments,
                path,
                basis,
                on: day.Date);
            AddAmount(
                figures,
                PlanningForecastFigureKind.DayLivingSpending,
                day.LivingSpending,
                path,
                basis,
                on: day.Date);
        }
    }

    /// <summary>
    /// Copies one cash window's start, end, and lowest cash.
    /// </summary>
    private static void AddWindow(
        List<PlanningForecastFigure> figures,
        PayoffRolloverKind path,
        PlanningForecastBasis basis,
        int horizonMonths,
        PlanCashWindowDto window)
    {
        AddDate(
            figures,
            PlanningForecastFigureKind.WindowFrom,
            window.From,
            path,
            basis: basis,
            horizonMonths: horizonMonths);
        AddAmount(
            figures,
            PlanningForecastFigureKind.EndingCash,
            window.EndingCash,
            path,
            basis,
            horizonMonths,
            window.Through);
        AddAmount(
            figures,
            PlanningForecastFigureKind.LowestCash,
            window.LowestCash,
            path,
            basis,
            horizonMonths,
            window.LowestCashOn);
    }

    /// <summary>
    /// Copies spendable cash for one path, including the guardrail amount when it is active.
    /// </summary>
    private static void AddSpendable(
        List<PlanningForecastFigure> figures,
        PayoffRolloverKind path,
        SpendableEstimateDto estimate)
    {
        AddAmount(figures, PlanningForecastFigureKind.Spendable, estimate.Spendable, path);
        AddAmount(
            figures,
            PlanningForecastFigureKind.LowestAvailable,
            estimate.LowestAvailable,
            path,
            on: estimate.LowestOn);
        AddDate(figures, PlanningForecastFigureKind.SpendableFrom, estimate.From, path);
        AddDate(figures, PlanningForecastFigureKind.SpendableThrough, estimate.Through, path);
        AddAmount(figures, PlanningForecastFigureKind.ReliableIncome, estimate.ReliableIncome, path);
        AddAmount(figures, PlanningForecastFigureKind.SpendableObligations, estimate.Obligations, path);
        AddAmount(figures, PlanningForecastFigureKind.OrdinarySpending, estimate.OrdinarySpending, path);
        AddAmount(figures, PlanningForecastFigureKind.Reserved, estimate.Reserved, path);
        if (!estimate.Guardrail.Active)
        {
            return;
        }

        AddAmount(
            figures,
            PlanningForecastFigureKind.GuardrailShortfall,
            estimate.Guardrail.Shortfall,
            path,
            on: estimate.Guardrail.On);
    }

    /// <summary>
    /// The labels the wording needs: currency, gaps, guardrails, orders, and spendable omissions.
    /// </summary>
    private static PlanningForecastContext MapContext(PlanRecoveryDto recovery)
    {
        return new PlanningForecastContext(
            recovery.PlanningCurrency,
            recovery.Priority,
            recovery.HasDebts,
            recovery.MissingBalance.Select(debt => debt.Name).Where(name => !string.IsNullOrWhiteSpace(name)).ToList(),
            recovery.ExcludedCurrencies,
            Guardrails(recovery),
            recovery.PriorityAlternatives
                .Select(alternative => new PlanningForecastPriority(
                    alternative.Choice,
                    alternative.Applied,
                    alternative.ChangesOrder))
                .ToList(),
            Uncertainties(recovery));
    }

    /// <summary>
    /// Guardrails the calculation marked active. An inactive extra is omitted.
    /// </summary>
    private static List<PlanningForecastGuardrail> Guardrails(PlanRecoveryDto recovery)
    {
        return Paths(recovery)
            .Where(path => path.Estimate.Guardrail.Active)
            .Select(path => new PlanningForecastGuardrail(
                path.Kind,
                path.Estimate.Guardrail.OrdinaryExpenses,
                path.Estimate.Guardrail.NearTermObligations,
                path.Estimate.Guardrail.AlreadyShortWithoutExtra))
            .ToList();
    }

    /// <summary>
    /// Spendable omissions, in path order. A blank name stays null.
    /// </summary>
    private static List<PlanningForecastUncertainty> Uncertainties(PlanRecoveryDto recovery)
    {
        return Paths(recovery)
            .SelectMany(path => path.Estimate.Uncertainties.Select(uncertainty =>
                new PlanningForecastUncertainty(
                    path.Kind,
                    uncertainty.Kind,
                    string.IsNullOrWhiteSpace(uncertainty.Name) ? null : uncertainty.Name)))
            .ToList();
    }

    /// <summary>
    /// The three freed-cash choices in the order the forecast explains them.
    /// </summary>
    private static IEnumerable<(PayoffRolloverKind Kind, SpendableEstimateDto Estimate)> Paths(
        PlanRecoveryDto recovery)
    {
        yield return (PayoffRolloverKind.Rollover, recovery.Spendable.Rollover);
        yield return (PayoffRolloverKind.Reclaim, recovery.Spendable.Reclaim);
        yield return (PayoffRolloverKind.ReclaimAll, recovery.Spendable.ReclaimAll);
    }

    /// <summary>
    /// Copies the forecast into the response shape.
    /// </summary>
    private static PlanningForecastDto ToDto(PlanningForecast forecast)
    {
        return new PlanningForecastDto
        {
            CalculationVersion = forecast.CalculationVersion,
            PlanningCurrency = forecast.PlanningCurrency,
            Figures = forecast.Figures.Select(ToDto).ToList(),
            Assumptions = forecast.Assumptions
                .Select(assumption => new PlanningForecastAssumptionDto
                {
                    Term = assumption.Term,
                    Detail = assumption.Detail
                })
                .ToList(),
            Tradeoffs = forecast.Tradeoffs.Select(ToDto).ToList(),
            Names = forecast.Names,
            Statement = forecast.Statement
        };
    }

    /// <summary>
    /// Copies one figure into the response shape.
    /// </summary>
    private static PlanningForecastFigureDto ToDto(PlanningForecastFigure figure)
    {
        return new PlanningForecastFigureDto
        {
            Kind = figure.Kind,
            Path = figure.Path,
            Priority = figure.Priority,
            Basis = figure.Basis,
            Amount = figure.Amount,
            On = figure.On,
            Count = figure.Count,
            HorizonMonths = figure.HorizonMonths,
            Name = figure.Name
        };
    }

    /// <summary>
    /// Copies one tradeoff into the response shape.
    /// </summary>
    private static PlanningForecastTradeoffDto ToDto(PlanningForecastTradeoff tradeoff)
    {
        return new PlanningForecastTradeoffDto
        {
            Kind = tradeoff.Kind,
            Path = tradeoff.Path,
            Priority = tradeoff.Priority,
            Basis = tradeoff.Basis,
            Explanation = tradeoff.Explanation
        };
    }

    /// <summary>
    /// Adds an amount the calculation returned.
    /// </summary>
    private static void AddAmount(
        List<PlanningForecastFigure> figures,
        PlanningForecastFigureKind kind,
        decimal amount,
        PayoffRolloverKind? path = null,
        PlanningForecastBasis basis = PlanningForecastBasis.Typical,
        int? horizonMonths = null,
        DateOnly? on = null,
        PayoffPriorityChoice? priority = null,
        string? name = null)
    {
        figures.Add(new PlanningForecastFigure(
            kind,
            path,
            priority,
            basis,
            amount,
            on,
            null,
            horizonMonths,
            name));
    }

    /// <summary>
    /// Adds an amount only when the calculation supplied one.
    /// </summary>
    private static void AddOptionalAmount(
        List<PlanningForecastFigure> figures,
        PlanningForecastFigureKind kind,
        decimal? amount,
        PayoffRolloverKind? path = null,
        PlanningForecastBasis basis = PlanningForecastBasis.Typical,
        int? horizonMonths = null,
        string? name = null)
    {
        if (amount is decimal value)
        {
            AddAmount(figures, kind, value, path, basis, horizonMonths, name: name);
        }
    }

    /// <summary>
    /// Adds a date the calculation returned.
    /// </summary>
    private static void AddDate(
        List<PlanningForecastFigure> figures,
        PlanningForecastFigureKind kind,
        DateOnly on,
        PayoffRolloverKind? path = null,
        string? name = null,
        PlanningForecastBasis basis = PlanningForecastBasis.Typical,
        int? horizonMonths = null)
    {
        figures.Add(new PlanningForecastFigure(
            kind,
            path,
            null,
            basis,
            null,
            on,
            null,
            horizonMonths,
            name));
    }

    /// <summary>
    /// Adds a date only when the calculation supplied one.
    /// </summary>
    private static void AddOptionalDate(
        List<PlanningForecastFigure> figures,
        PlanningForecastFigureKind kind,
        DateOnly? on,
        PayoffRolloverKind path,
        PlanningForecastBasis basis)
    {
        if (on is DateOnly date)
        {
            AddDate(figures, kind, date, path, basis: basis);
        }
    }

    /// <summary>
    /// Adds a count the calculation returned.
    /// </summary>
    private static void AddCount(
        List<PlanningForecastFigure> figures,
        PlanningForecastFigureKind kind,
        int count,
        PayoffRolloverKind? path = null,
        PlanningForecastBasis basis = PlanningForecastBasis.Typical,
        int? horizonMonths = null)
    {
        figures.Add(new PlanningForecastFigure(
            kind,
            path,
            null,
            basis,
            null,
            null,
            count,
            horizonMonths,
            null));
    }

    #endregion
}
