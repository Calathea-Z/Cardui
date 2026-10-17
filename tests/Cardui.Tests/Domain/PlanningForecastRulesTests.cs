using Cardui.Api.Domain.Planning;
using Cardui.Api.Domain.Recovery;
using Xunit;

namespace Cardui.Tests.Domain;

public class PlanningForecastRulesTests
{
    private static readonly DateOnly Shortfall = new(2026, 11, 1);

    private static readonly DateOnly Paid = new(2027, 1, 15);

    private static readonly DateOnly Later = new(2027, 6, 1);

    [Fact]
    public void Compose_CopiesTheCalculationAndKeepsBothSides()
    {
        var forecast = PlanningForecastRules.Compose(SampleFigures(), SampleContext());

        Assert.Equal(PlanningForecastRules.CalculationVersion, forecast.CalculationVersion);
        Assert.Equal(10m, Amount(forecast, PlanningForecastFigureKind.TotalInterest, PayoffRolloverKind.Rollover));
        Assert.Equal(12.50m, Amount(forecast, PlanningForecastFigureKind.TotalInterest, PayoffRolloverKind.Reclaim));
        Assert.Equal(20m, Amount(forecast, PlanningForecastFigureKind.TotalInterest, PayoffRolloverKind.ReclaimAll));
        Assert.Equal(Paid, Date(forecast, PlanningForecastFigureKind.PaidOffOn, PayoffRolloverKind.Rollover));
        Assert.Null(Date(forecast, PlanningForecastFigureKind.PaidOffOn, PayoffRolloverKind.Reclaim));
        Assert.Contains("10.00 USD", forecast.Statement, StringComparison.Ordinal);
        Assert.Contains("12.50 USD", forecast.Statement, StringComparison.Ordinal);
        Assert.Contains("20.00 USD", forecast.Statement, StringComparison.Ordinal);
        Assert.Contains("2027-01-15", forecast.Statement, StringComparison.Ordinal);
        Assert.Contains("not projected", forecast.Statement, StringComparison.Ordinal);
        Assert.Contains("-40.00 USD", forecast.Statement, StringComparison.Ordinal);
        Assert.Contains("80.00 USD a month for flexible spending", forecast.Statement, StringComparison.Ordinal);
        Assert.Contains("2 remaining minimums are unknown", forecast.Statement, StringComparison.Ordinal);
        Assert.Contains("Store card has no balance", forecast.Statement, StringComparison.Ordinal);
        Assert.Contains("not zero", forecast.Statement, StringComparison.Ordinal);
        Assert.DoesNotContain("surplus", forecast.Statement, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Interest", Terms(forecast));
        Assert.Contains("Spendable", Terms(forecast));
        Assert.Contains("Low pay", Terms(forecast));
        Assert.Contains("Currency", Terms(forecast));
        Assert.Contains("Limit", Terms(forecast));
        Assert.Contains("Spending", Terms(forecast));
        Assert.Contains("flexible spending", Detail(forecast, "Spending"), StringComparison.Ordinal);
        var freed = Explanation(forecast, PlanningForecastTradeoffKind.FreedCash);
        Assert.Contains("10.00 USD", freed, StringComparison.Ordinal);
        Assert.Contains("20.00 USD", freed, StringComparison.Ordinal);
        Assert.Contains("saving or spending", freed, StringComparison.Ordinal);
        Assert.Contains(
            "3.25 USD",
            Explanation(forecast, PlanningForecastTradeoffKind.PayoffPriority, PayoffPriorityChoice.MinimumRelease),
            StringComparison.Ordinal);
        Assert.Contains(Shortfall.ToString("yyyy-MM-dd"), forecast.Statement, StringComparison.Ordinal);
        Assert.Contains("stays an estimate", forecast.Statement, StringComparison.Ordinal);
        Assert.Contains("ordinary spending", Explanation(forecast, PlanningForecastTradeoffKind.ExtraPayment), StringComparison.Ordinal);
        AssertPlain(forecast);
    }

    [Fact]
    public void Compose_LeavesAnUnknownMinimumUnknown()
    {
        var figures = new List<PlanningForecastFigure>
        {
            Amount(PlanningForecastFigureKind.TotalInterest, 0m, PayoffRolloverKind.Rollover),
            Amount(PlanningForecastFigureKind.RecurringRoom, 0m, PayoffRolloverKind.Rollover),
            Amount(PlanningForecastFigureKind.CashReclaimed, 0m, PayoffRolloverKind.Rollover),
            Amount(PlanningForecastFigureKind.CashRolled, 0m, PayoffRolloverKind.Rollover)
        };

        var forecast = PlanningForecastRules.Compose(figures, SampleContext());

        Assert.Contains("Known minimums are unknown.", forecast.Statement, StringComparison.Ordinal);
        Assert.Contains("Known minimums still due are unknown.", forecast.Statement, StringComparison.Ordinal);
        Assert.DoesNotContain("Known minimums start at", forecast.Statement, StringComparison.Ordinal);
        Assert.Contains("not projected", forecast.Statement, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_NamesALowPayShortfallWithoutADebt()
    {
        var figures = new List<PlanningForecastFigure>
        {
            Date(PlanningForecastFigureKind.ShortfallOn, Shortfall, PayoffRolloverKind.Rollover, PlanningForecastBasis.LowPay)
        };
        var context = SampleContext() with { HasDebts = false };

        var forecast = PlanningForecastRules.Compose(figures, context);
        var tradeoff = Assert.Single(
            forecast.Tradeoffs,
            item => item.Kind == PlanningForecastTradeoffKind.Shortfall);

        Assert.Contains("No debt is recorded.", forecast.Statement, StringComparison.Ordinal);
        Assert.Contains("at low pay", tradeoff.Explanation, StringComparison.Ordinal);
        Assert.Equal(PlanningForecastBasis.LowPay, tradeoff.Basis);
        Assert.DoesNotContain("payoff date is 2026-11-01", forecast.Statement, StringComparison.Ordinal);
        AssertPlain(forecast);
    }

    [Fact]
    public void Compose_KeepsAnIncomeLossCountAndTheSpendingAmount()
    {
        var figures = SampleFigures();
        figures.Add(new PlanningForecastFigure(
            PlanningForecastFigureKind.IncomeLossMonths,
            null,
            null,
            PlanningForecastBasis.Typical,
            null,
            null,
            2,
            null,
            null));

        var forecast = PlanningForecastRules.Compose(figures, SampleContext());

        Assert.Contains("The next 2 months leave out included pay.", Detail(forecast, "Income loss"), StringComparison.Ordinal);
        Assert.Contains("These cash changes do not change which debt is paid first.", Detail(forecast, "Payoff order"), StringComparison.Ordinal);
        Assert.Null(PlanningForecastRules.Reject(Detail(forecast, "Income loss"), forecast));
        Assert.Null(PlanningForecastRules.Reject("The gap lasts 2 months.", forecast));
        Assert.Equal("That number is not in the calculation.", PlanningForecastRules.Reject("The gap lasts 3 months.", forecast));
    }

    [Fact]
    public void Compose_KeepsAWindfallAndACashFloor()
    {
        var figures = new List<PlanningForecastFigure>
        {
            Amount(PlanningForecastFigureKind.WindfallAmount, 500m),
            Date(PlanningForecastFigureKind.WindfallOn, new DateOnly(2026, 12, 1)),
            Amount(PlanningForecastFigureKind.CashToKeep, 1000m)
        };

        var forecast = PlanningForecastRules.Compose(figures, SampleContext() with { HasDebts = false });

        Assert.Contains(
            "500.00 USD on 2026-12-01 stays in cash and does not pay a debt.",
            Detail(forecast, "Windfall"),
            StringComparison.Ordinal);
        Assert.Contains("protects 1000.00 USD", Detail(forecast, "Cash to keep"), StringComparison.Ordinal);
        Assert.Contains(
            "These cash changes do not change which debt is paid first.",
            Detail(forecast, "Payoff order"),
            StringComparison.Ordinal);
        AssertPlain(forecast);
    }

    [Fact]
    public void Reject_IgnoresADigitInsideACopiedName()
    {
        var forecast = PlanningForecastRules.Compose(
            [],
            SampleContext() with
            {
                HasDebts = false,
                MissingBalanceNames = ["Visa 41"]
            });

        Assert.Contains("Visa 41", forecast.Statement, StringComparison.Ordinal);
        Assert.Contains("Visa 41", forecast.Names);
        Assert.Null(PlanningForecastRules.Reject(forecast.Statement, forecast));
        Assert.Equal(
            "That number is not in the calculation.",
            PlanningForecastRules.Reject("The balance is 41.", forecast));
    }

    [Fact]
    public void Reject_AcceptsTheForecastAndRejectsAnInventedClaim()
    {
        var forecast = PlanningForecastRules.Compose(SampleFigures(), SampleContext());

        Assert.Null(PlanningForecastRules.Reject(forecast.Statement, forecast));
        Assert.Null(PlanningForecastRules.Reject("Starting cash is 100.", forecast));
        Assert.Null(PlanningForecastRules.Reject("At 12 months.", forecast));
        Assert.Equal("State the forecast.", PlanningForecastRules.Reject("  ", forecast));
        Assert.Equal(
            "That amount is not in the calculation.",
            PlanningForecastRules.Reject("Interest is 9,999.99 USD.", forecast));
        Assert.Equal(
            "That date is not in the calculation.",
            PlanningForecastRules.Reject("Paid off on 2031-01-01.", forecast));
        Assert.Equal(
            "That number is not in the calculation.",
            PlanningForecastRules.Reject("At 12 percent.", forecast));
        Assert.Equal(
            "A forecast cannot guarantee a result.",
            PlanningForecastRules.Reject("You will be debt-free.", forecast));
        Assert.Equal(
            "A forecast cannot add a term the calculation does not use.",
            PlanningForecastRules.Reject("The financial health score is fine.", forecast));
        Assert.Equal(
            "A forecast cannot add a term the calculation does not use.",
            PlanningForecastRules.Reject("Use the snowball order.", forecast));
        Assert.Equal(
            "A forecast cannot describe this as a personal failing.",
            PlanningForecastRules.Reject("That spending is irresponsible.", forecast));
        Assert.Equal(
            "A forecast cannot treat one result as automatically better.",
            PlanningForecastRules.Reject("A lower payment is automatically better.", forecast));
    }

    #region Private Methods

    /// <summary>
    /// Rejects the forecast's own wording when it promises, invents a term, shames, or cites a number it did not copy.
    /// </summary>
    private static void AssertPlain(PlanningForecast forecast)
    {
        Assert.Null(PlanningForecastRules.Reject(forecast.Statement, forecast));
        foreach (var assumption in forecast.Assumptions)
        {
            Assert.Null(PlanningForecastRules.Reject(assumption.Detail, forecast));
        }

        foreach (var tradeoff in forecast.Tradeoffs)
        {
            Assert.Null(PlanningForecastRules.Reject(tradeoff.Explanation, forecast));
        }

        Assert.DoesNotContain("guaranteed", forecast.Statement, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("you will", forecast.Statement, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("score", forecast.Statement, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("shame", forecast.Statement, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("irresponsible", forecast.Statement, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("waste", forecast.Statement, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("failure", forecast.Statement, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("automatically better", forecast.Statement, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("better deal", forecast.Statement, StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> Terms(PlanningForecast forecast)
    {
        return forecast.Assumptions.Select(assumption => assumption.Term).ToList();
    }

    private static string Detail(PlanningForecast forecast, string term)
    {
        return Assert.Single(forecast.Assumptions, assumption => assumption.Term == term).Detail;
    }

    private static string Explanation(
        PlanningForecast forecast,
        PlanningForecastTradeoffKind kind,
        PayoffPriorityChoice? priority = null)
    {
        return Assert.Single(
            forecast.Tradeoffs,
            tradeoff => tradeoff.Kind == kind && tradeoff.Priority == priority).Explanation;
    }

    private static decimal? Amount(
        PlanningForecast forecast,
        PlanningForecastFigureKind kind,
        PayoffRolloverKind path)
    {
        return Assert.Single(
            forecast.Figures,
            figure => figure.Kind == kind && figure.Path == path).Amount;
    }

    private static DateOnly? Date(
        PlanningForecast forecast,
        PlanningForecastFigureKind kind,
        PayoffRolloverKind path)
    {
        return forecast.Figures.SingleOrDefault(figure => figure.Kind == kind && figure.Path == path)?.On;
    }

    private static List<PlanningForecastFigure> SampleFigures()
    {
        var figures = new List<PlanningForecastFigure>
        {
            Amount(PlanningForecastFigureKind.MonthlyExtra, 0m),
            Amount(PlanningForecastFigureKind.ReclaimAmount, 10m),
            Amount(PlanningForecastFigureKind.FlexibleSpending, 80m),
            Amount(PlanningForecastFigureKind.LivingSpending, 40m),
            Amount(PlanningForecastFigureKind.SavedCashFloor, 0m),
            Amount(PlanningForecastFigureKind.StartingCash, 100m)
        };
        figures.AddRange(Path(PayoffRolloverKind.Rollover, 10m, 25m, 0m, 25m, 40m, 25m, Paid));
        figures.AddRange(Path(PayoffRolloverKind.Reclaim, 12.50m, 15m, 10m, 15m, 40m, 25m, null));
        figures.AddRange(Path(PayoffRolloverKind.ReclaimAll, 20m, 0m, 25m, 0m, 40m, 0m, Later));
        figures.AddRange(Window(0, 10m, -40m, Shortfall));
        figures.AddRange(Window(6, 50m, -5m, new DateOnly(2026, 12, 1)));
        figures.Add(new PlanningForecastFigure(
            PlanningForecastFigureKind.UnknownMinimumCount,
            PayoffRolloverKind.Rollover,
            null,
            PlanningForecastBasis.Typical,
            null,
            null,
            2,
            6,
            null));
        figures.Add(Date(PlanningForecastFigureKind.ShortfallOn, Shortfall, PayoffRolloverKind.Rollover));
        figures.Add(Amount(PlanningForecastFigureKind.Spendable, 30m, PayoffRolloverKind.Rollover));
        figures.Add(Amount(
            PlanningForecastFigureKind.LowestAvailable,
            -40m,
            PayoffRolloverKind.Rollover,
            on: Shortfall));
        figures.Add(Amount(PlanningForecastFigureKind.ReliableIncome, 0m, PayoffRolloverKind.Rollover));
        figures.Add(Amount(PlanningForecastFigureKind.SpendableObligations, 20m, PayoffRolloverKind.Rollover));
        figures.Add(Amount(PlanningForecastFigureKind.OrdinarySpending, 80m, PayoffRolloverKind.Rollover));
        figures.Add(Amount(PlanningForecastFigureKind.Reserved, 0m, PayoffRolloverKind.Rollover));
        figures.Add(Amount(
            PlanningForecastFigureKind.GuardrailShortfall,
            15m,
            PayoffRolloverKind.Rollover,
            on: new DateOnly(2026, 11, 2)));
        figures.Add(Amount(PlanningForecastFigureKind.InterestDifference, 0m, priority: PayoffPriorityChoice.Avalanche));
        figures.Add(Amount(PlanningForecastFigureKind.TradeoffCash, 0m, priority: PayoffPriorityChoice.Avalanche));
        figures.Add(Amount(
            PlanningForecastFigureKind.InterestDifference,
            3.25m,
            priority: PayoffPriorityChoice.MinimumRelease));
        figures.Add(Amount(PlanningForecastFigureKind.TradeoffCash, 15m, priority: PayoffPriorityChoice.MinimumRelease));
        return figures;
    }

    private static IEnumerable<PlanningForecastFigure> Path(
        PayoffRolloverKind path,
        decimal interest,
        decimal room,
        decimal kept,
        decimal rolled,
        decimal starting,
        decimal remaining,
        DateOnly? paid)
    {
        yield return Amount(PlanningForecastFigureKind.TotalInterest, interest, path);
        yield return Amount(PlanningForecastFigureKind.RecurringRoom, room, path);
        yield return Amount(PlanningForecastFigureKind.CashReclaimed, kept, path);
        yield return Amount(PlanningForecastFigureKind.CashRolled, rolled, path);
        yield return Amount(PlanningForecastFigureKind.StartingObligation, starting, path);
        yield return Amount(PlanningForecastFigureKind.RemainingObligation, remaining, path);
        if (paid is DateOnly date)
        {
            yield return Date(PlanningForecastFigureKind.PaidOffOn, date, path);
        }
    }

    private static IEnumerable<PlanningForecastFigure> Window(
        int horizon,
        decimal ending,
        decimal lowest,
        DateOnly lowestOn)
    {
        yield return Amount(
            PlanningForecastFigureKind.EndingCash,
            ending,
            PayoffRolloverKind.Rollover,
            horizon: horizon);
        yield return Amount(
            PlanningForecastFigureKind.LowestCash,
            lowest,
            PayoffRolloverKind.Rollover,
            horizon: horizon,
            on: lowestOn);
    }

    private static PlanningForecastFigure Amount(
        PlanningForecastFigureKind kind,
        decimal amount,
        PayoffRolloverKind? path = null,
        PayoffPriorityChoice? priority = null,
        PlanningForecastBasis basis = PlanningForecastBasis.Typical,
        int? horizon = null,
        DateOnly? on = null)
    {
        return new PlanningForecastFigure(kind, path, priority, basis, amount, on, null, horizon, null);
    }

    private static PlanningForecastFigure Date(
        PlanningForecastFigureKind kind,
        DateOnly on,
        PayoffRolloverKind? path = null,
        PlanningForecastBasis basis = PlanningForecastBasis.Typical)
    {
        return new PlanningForecastFigure(kind, path, null, basis, null, on, null, null, null);
    }

    private static PlanningForecastContext SampleContext()
    {
        return new PlanningForecastContext(
            "USD",
            PayoffPriorityChoice.Avalanche,
            true,
            ["Store card"],
            [],
            [
                new PlanningForecastGuardrail(PayoffRolloverKind.Rollover, true, false, false)
            ],
            [
                new PlanningForecastPriority(PayoffPriorityChoice.Avalanche, true, false),
                new PlanningForecastPriority(PayoffPriorityChoice.MinimumRelease, false, true)
            ],
            [
                new PlanningForecastUncertainty(
                    PayoffRolloverKind.Rollover,
                    SpendableUncertaintyKind.VariableIncome,
                    "Side job")
            ]);
    }

    #endregion
}
