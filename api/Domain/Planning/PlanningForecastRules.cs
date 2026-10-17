using System.Globalization;
using System.Text.RegularExpressions;
using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Domain.Planning;

/// <summary>
/// Builds a forecast from numbers the plan calculation already produced, and rejects a claim that adds one.
/// The wording keeps the assumptions and names both sides of a tradeoff.
/// It does not promise a date, add a term the calculation does not use, or treat ordinary spending as a failing.
/// </summary>
public static class PlanningForecastRules
{
    /// <summary>
    /// The wording set. Raise it when a figure's meaning or a sentence's claim changes.
    /// </summary>
    public const string CalculationVersion = "1";

    private const string Guarantee = "A forecast cannot guarantee a result.";
    private const string Invented = "A forecast cannot add a term the calculation does not use.";
    private const string Shame = "A forecast cannot describe this as a personal failing.";
    private const string Ranked = "A forecast cannot treat one result as automatically better.";

    private static readonly PayoffRolloverKind[] Paths =
    [
        PayoffRolloverKind.Rollover,
        PayoffRolloverKind.Reclaim,
        PayoffRolloverKind.ReclaimAll
    ];

    private static readonly int[] Windows = [0, 6, 12, 18];

    private static readonly (string Needle, string Error, bool Word)[] Banned =
    [
        ("guaranteed", Guarantee, false),
        ("guarantee", Guarantee, false),
        ("you will", Guarantee, false),
        ("you'll", Guarantee, false),
        ("you’ll", Guarantee, false),
        ("definitely", Guarantee, false),
        ("promised", Guarantee, false),
        ("promise", Guarantee, false),
        ("certain to", Guarantee, false),
        ("sure to", Guarantee, false),
        ("no matter what", Guarantee, false),
        ("financial health", Invented, false),
        ("wellness", Invented, false),
        ("credit score", Invented, false),
        ("snowball", Invented, false),
        ("automatically better", Ranked, false),
        ("better deal", Ranked, false),
        ("score", Invented, true),
        ("grade", Invented, true),
        ("rating", Invented, true),
        ("ashamed", Shame, false),
        ("irresponsible", Shame, false),
        ("wasteful", Shame, false),
        ("you failed", Shame, false),
        ("bad spending", Shame, false),
        ("cut all", Shame, false),
        ("stop enjoying", Shame, false),
        ("shame", Shame, true),
        ("waste", Shame, true),
        ("stupid", Shame, true),
        ("lazy", Shame, true),
        ("guilty", Shame, true),
        ("failure", Shame, true)
    ];

    private static readonly Regex DatePattern = new(
        @"\d{4}-\d{2}-\d{2}",
        RegexOptions.CultureInvariant);

    private static readonly Regex MoneyPattern = new(
        @"-?\d+\.\d{2}",
        RegexOptions.CultureInvariant);

    private static readonly Regex IntegerPattern = new(
        @"\d+",
        RegexOptions.CultureInvariant);

    private static readonly Regex CommaPattern = new(
        @"(\d),(?=\d)",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// Builds the forecast from figures copied out of the plan calculation.
    /// A missing payoff date stays unprojected. A missing minimum stays unknown. Neither is written as zero.
    /// </summary>
    public static PlanningForecast Compose(
        IReadOnlyList<PlanningForecastFigure> figures,
        PlanningForecastContext context)
    {
        var assumptions = Assumptions(figures, context);
        var tradeoffs = Tradeoffs(figures, context);
        var names = Names(figures, context);
        var statement = Statement(figures, context, tradeoffs);
        return new PlanningForecast(
            CalculationVersion,
            context.PlanningCurrency,
            figures,
            assumptions,
            tradeoffs,
            names,
            statement);
    }

    /// <summary>
    /// Rejects a claim whose amount, date, or other number is not in the forecast, or whose wording promises, invents a term, or shames.
    /// Six, 12, 18, 30, and 50 may be named as the horizons and the year limit. A percent after one of those still has to be a calculated amount.
    /// </summary>
    public static string? Reject(string? claim, PlanningForecast forecast)
    {
        if (string.IsNullOrWhiteSpace(claim))
        {
            return "State the forecast.";
        }

        var language = RejectLanguage(claim);
        if (language is not null)
        {
            return language;
        }

        return RejectNumbers(WithoutNames(claim, forecast.Names), forecast.Figures);
    }

    #region Private Methods

    /// <summary>
    /// The calculation rules, in the order Plan already shows them, plus a scenario rule when one was applied.
    /// </summary>
    private static IReadOnlyList<PlanningForecastAssumption> Assumptions(
        IReadOnlyList<PlanningForecastFigure> figures,
        PlanningForecastContext context)
    {
        var currency = Currency(context.PlanningCurrency);
        var assumptions = new List<PlanningForecastAssumption>
        {
            new("Order", Order(figures, context, currency)),
            new("Payments", "Each debt pays its minimum on its monthly due date."),
            new("Interest", "One month of interest at the debt's rate, or its promo rate until that ends."),
            new("Payoff month", "The month a debt is paid off still pays it. Its payment is free the month after."),
            new(
                "Due dates",
                "A due date that has passed moves ahead a month at a time to the first one on or after today."),
            new(
                "Roll payments forward",
                "A freed payment goes to the next debt the month after it ends. It becomes breathing room once no debt can take it."),
            new("Keep some", KeepSome(figures, currency)),
            new(
                "Free up cash",
                "A freed payment becomes breathing room the month after it ends. It is not sent to the next debt."),
            new(
                "Cash outlook",
                "Starts with cash on Accounts, adds shared pay, and takes out bills, flexible spending, and debt payments. Set-aside money stays in cash."),
            new(
                "Spendable",
                "The lowest cash left after set-aside money, before extra debt payments. Only steady pay counts. It is not a monthly surplus."),
            new(
                "Low pay",
                "Uses each income source's low amount where one is recorded, and leaves out expected raises."),
            new("Left out", LeftOut(context)),
            new("Currency", CurrencyRule(context, currency)),
            new("Limit", "Projections stop at 50 years.")
        };
        assumptions.AddRange(ScenarioAssumptions(figures, currency));
        return assumptions;
    }

    /// <summary>
    /// Who is paid first, and the extra the calculation applied. The sentence does not give a payoff date.
    /// </summary>
    private static string Order(
        IReadOnlyList<PlanningForecastFigure> figures,
        PlanningForecastContext context,
        string currency)
    {
        var extra = AmountOf(figures, PlanningForecastFigureKind.MonthlyExtra);
        var extraSentence = extra is decimal amount && amount > 0
            ? Money(amount, currency) + " extra each month goes to the first debt that can take it."
            : "No extra payment yet.";
        var lead = context.Priority switch
        {
            PayoffPriorityChoice.MinimumRelease => "Remove a minimum first.",
            PayoffPriorityChoice.Utilization => "Lower utilization first.",
            PayoffPriorityChoice.Constraint => "Pay a chosen debt first or last.",
            _ => "Highest interest rate first."
        };
        var tail = context.Priority == PayoffPriorityChoice.Avalanche
            ? "Who is paid first, not when each debt ends."
            : "Not the payoff date.";
        return lead + " " + extraSentence + " " + tail;
    }

    /// <summary>
    /// How much of a freed payment the partial path keeps. Zero names the choice without pretending an amount was requested.
    /// </summary>
    private static string KeepSome(IReadOnlyList<PlanningForecastFigure> figures, string currency)
    {
        var reclaim = AmountOf(figures, PlanningForecastFigureKind.ReclaimAmount);
        return reclaim is decimal amount && amount > 0
            ? "Keep " + Money(amount, currency) + " each month after the payment ends. The rest rolls."
            : "Keep a chosen amount each month after the payment ends. The rest rolls.";
    }

    /// <summary>
    /// Names a debt with no balance, and says that gap is not a zero.
    /// </summary>
    private static string LeftOut(PlanningForecastContext context)
    {
        var rule =
            "A debt missing a balance, rate, minimum, or due date, or one whose payment does not cover its interest, is left out of the payoff.";
        if (context.MissingBalanceNames.Count == 0)
        {
            return rule;
        }

        return rule
            + " "
            + string.Join(", ", context.MissingBalanceNames)
            + " has no balance. A missing balance is not zero.";
    }

    /// <summary>
    /// The planning currency, and any code the calculation left out.
    /// </summary>
    private static string CurrencyRule(PlanningForecastContext context, string currency)
    {
        var counted = "Only " + currency + " is counted.";
        if (context.ExcludedCurrencies.Count == 0)
        {
            return counted;
        }

        return "Amounts in " + string.Join(", ", context.ExcludedCurrencies) + " are left out. " + counted;
    }

    /// <summary>
    /// Scenario rules for amounts the calculation applied. A cash change does not change who is paid first.
    /// </summary>
    private static IEnumerable<PlanningForecastAssumption> ScenarioAssumptions(
        IReadOnlyList<PlanningForecastFigure> figures,
        string currency)
    {
        var added = false;
        var months = CountOf(figures, PlanningForecastFigureKind.IncomeLossMonths);
        if (months is int count && count > 0)
        {
            added = true;
            var word = count == 1 ? "month" : "months";
            yield return new PlanningForecastAssumption(
                "Income loss",
                "The next " + count + " " + word + " leave out included pay. Later pay stays on its dates.");
        }

        var windfall = AmountOf(figures, PlanningForecastFigureKind.WindfallAmount);
        var windfallOn = DateOf(figures, PlanningForecastFigureKind.WindfallOn);
        if (windfall is decimal windfallAmount && windfallAmount > 0 && windfallOn is DateOnly on)
        {
            added = true;
            yield return new PlanningForecastAssumption(
                "Windfall",
                Money(windfallAmount, currency) + " on " + When(on) + " stays in cash and does not pay a debt.");
        }

        var spending = AmountOf(figures, PlanningForecastFigureKind.FlexibleSpending);
        if (spending is decimal flexible)
        {
            added = true;
            yield return new PlanningForecastAssumption(
                "Spending",
                "This estimate uses "
                + Money(flexible, currency)
                + " a month for flexible spending. The saved amount is unchanged.");
        }

        var floor = AmountOf(figures, PlanningForecastFigureKind.CashToKeep);
        if (floor is decimal cashToKeep)
        {
            added = true;
            yield return new PlanningForecastAssumption(
                "Cash to keep",
                "This estimate protects " + Money(cashToKeep, currency) + ". Other set-aside money stays in cash.");
        }

        if (added)
        {
            yield return new PlanningForecastAssumption(
                "Payoff order",
                "These cash changes do not change which debt is paid first.");
        }
    }

    /// <summary>
    /// The comparisons the calculation made: freed cash, payoff order, a shortfall, and an extra that leaves a gap.
    /// </summary>
    private static IReadOnlyList<PlanningForecastTradeoff> Tradeoffs(
        IReadOnlyList<PlanningForecastFigure> figures,
        PlanningForecastContext context)
    {
        var tradeoffs = new List<PlanningForecastTradeoff>();
        var freed = FreedCash(figures, context);
        if (freed is not null)
        {
            tradeoffs.Add(freed);
        }

        tradeoffs.AddRange(PriorityTradeoffs(figures, context));
        tradeoffs.AddRange(ShortfallTradeoffs(figures));
        tradeoffs.AddRange(ExtraTradeoffs(figures, context));
        return tradeoffs;
    }

    /// <summary>
    /// States the interest and the cash kept on each freed-cash choice. A lower interest total is one side, not the winner.
    /// </summary>
    private static PlanningForecastTradeoff? FreedCash(
        IReadOnlyList<PlanningForecastFigure> figures,
        PlanningForecastContext context)
    {
        if (!context.HasDebts)
        {
            return null;
        }

        var currency = Currency(context.PlanningCurrency);
        var sentences = new List<string>();
        foreach (var path in Paths)
        {
            var interest = AmountOf(figures, PlanningForecastFigureKind.TotalInterest, path);
            var kept = AmountOf(figures, PlanningForecastFigureKind.CashReclaimed, path);
            var rolled = AmountOf(figures, PlanningForecastFigureKind.CashRolled, path);
            if (interest is null && kept is null && rolled is null)
            {
                continue;
            }

            sentences.Add(FreedSentence(path, interest, kept, rolled, currency));
        }

        if (sentences.Count == 0)
        {
            return null;
        }

        sentences.Add(
            "A lower interest total is the side that sends freed cash to a later debt. A higher interest total is the side that keeps that cash for saving or spending. Flexible spending stays in the estimate.");
        return new PlanningForecastTradeoff(
            PlanningForecastTradeoffKind.FreedCash,
            null,
            null,
            null,
            string.Join(" ", sentences));
    }

    /// <summary>
    /// One freed-cash choice: its interest, what it keeps, and what it sends on.
    /// </summary>
    private static string FreedSentence(
        PayoffRolloverKind path,
        decimal? interest,
        decimal? kept,
        decimal? rolled,
        string currency)
    {
        return PathName(path)
            + " estimates "
            + (interest is decimal interestAmount ? Money(interestAmount, currency) : "an uncalculated amount")
            + " of interest, keeps "
            + (kept is decimal keptAmount ? Money(keptAmount, currency) : "an uncalculated amount")
            + ", and sends "
            + (rolled is decimal rolledAmount ? Money(rolledAmount, currency) : "an uncalculated amount")
            + " to a later debt.";
    }

    /// <summary>
    /// Each payoff order the calculation compared. Highest rate first stays available beside the others.
    /// </summary>
    private static IEnumerable<PlanningForecastTradeoff> PriorityTradeoffs(
        IReadOnlyList<PlanningForecastFigure> figures,
        PlanningForecastContext context)
    {
        var currency = Currency(context.PlanningCurrency);
        foreach (var priority in context.Priorities)
        {
            var interest = AmountOf(
                figures,
                PlanningForecastFigureKind.InterestDifference,
                priority: priority.Choice);
            var cash = AmountOf(
                figures,
                PlanningForecastFigureKind.TradeoffCash,
                priority: priority.Choice);
            if (interest is null && cash is null)
            {
                continue;
            }

            yield return new PlanningForecastTradeoff(
                PlanningForecastTradeoffKind.PayoffPriority,
                null,
                priority.Choice,
                null,
                PrioritySentence(priority, interest, cash, currency));
        }
    }

    /// <summary>
    /// The interest gap and the minimum-cash gap against highest rate first.
    /// A lower monthly payment is not treated as the reason to choose.
    /// </summary>
    private static string PrioritySentence(
        PlanningForecastPriority priority,
        decimal? interest,
        decimal? cash,
        string currency)
    {
        var label = PriorityName(priority.Choice);
        var follows = priority.Applied
            ? "This estimate follows " + label + "."
            : label + " stays available.";
        if (priority.Choice == PayoffPriorityChoice.Avalanche
            && interest is decimal avalancheInterest
            && avalancheInterest == 0
            && cash is decimal avalancheCash
            && avalancheCash == 0)
        {
            return follows + " The interest difference is " + Money(0, currency) + ".";
        }

        return follows
            + " The interest difference from highest rate first is "
            + (interest is decimal interestAmount ? Money(interestAmount, currency) : "not calculated")
            + ", and the net minimum cash is "
            + (cash is decimal cashAmount ? Money(cashAmount, currency) : "not calculated")
            + ". A lower monthly payment is not enough to choose one.";
    }

    /// <summary>
    /// A cash gap the calculation found, at typical pay and at low pay.
    /// The payoff date stays an estimate while the gap is visible.
    /// </summary>
    private static IEnumerable<PlanningForecastTradeoff> ShortfallTradeoffs(
        IReadOnlyList<PlanningForecastFigure> figures)
    {
        foreach (var path in Paths)
        {
            foreach (var basis in new[] { PlanningForecastBasis.Typical, PlanningForecastBasis.LowPay })
            {
                var cash = DateOf(figures, PlanningForecastFigureKind.ShortfallOn, path, basis: basis);
                var reserve = DateOf(
                    figures,
                    PlanningForecastFigureKind.ReserveShortfallOn,
                    path,
                    basis: basis);
                if (cash is null && reserve is null)
                {
                    continue;
                }

                yield return new PlanningForecastTradeoff(
                    PlanningForecastTradeoffKind.Shortfall,
                    path,
                    null,
                    basis,
                    ShortfallSentence(path, basis, cash, reserve));
            }
        }
    }

    /// <summary>
    /// Names the shortfall dates for one path and keeps the payoff date conditional.
    /// </summary>
    private static string ShortfallSentence(
        PayoffRolloverKind path,
        PlanningForecastBasis basis,
        DateOnly? cash,
        DateOnly? reserve)
    {
        var sentences = new List<string>
        {
            PathName(path) + (basis == PlanningForecastBasis.LowPay ? " at low pay:" : ":")
        };
        if (cash is DateOnly cashOn)
        {
            sentences.Add("Cash goes below zero on " + When(cashOn) + ".");
        }

        if (reserve is DateOnly reserveOn)
        {
            sentences.Add("Money left after protected savings goes below zero on " + When(reserveOn) + ".");
        }

        sentences.Add("The payoff date stays an estimate while that gap is visible.");
        return string.Join(" ", sentences);
    }

    /// <summary>
    /// An extra payment that leaves ordinary spending or a near-term bill short. The spending is not called a mistake.
    /// </summary>
    private static IEnumerable<PlanningForecastTradeoff> ExtraTradeoffs(
        IReadOnlyList<PlanningForecastFigure> figures,
        PlanningForecastContext context)
    {
        var currency = Currency(context.PlanningCurrency);
        foreach (var guardrail in context.Guardrails)
        {
            var amount = AmountOf(figures, PlanningForecastFigureKind.GuardrailShortfall, guardrail.Path);
            var on = DateOf(figures, PlanningForecastFigureKind.GuardrailShortfall, guardrail.Path);
            if (amount is null)
            {
                continue;
            }

            yield return new PlanningForecastTradeoff(
                PlanningForecastTradeoffKind.ExtraPayment,
                guardrail.Path,
                null,
                null,
                ExtraSentence(guardrail, amount.Value, on, currency));
        }
    }

    /// <summary>
    /// How far the extra leaves the named gap, and whether that gap was already there.
    /// </summary>
    private static string ExtraSentence(
        PlanningForecastGuardrail guardrail,
        decimal amount,
        DateOnly? on,
        string currency)
    {
        var gap = guardrail.OrdinaryExpenses && guardrail.NearTermObligations
            ? "ordinary spending and a near-term bill or debt payment"
            : guardrail.NearTermObligations
                ? "a near-term bill or debt payment"
                : "ordinary spending";
        var sentence = PathName(guardrail.Path)
            + ": the extra payment leaves "
            + gap
            + " short by "
            + Money(amount, currency);
        if (on is DateOnly date)
        {
            sentence += " on " + When(date);
        }

        sentence += ".";
        if (guardrail.AlreadyShort)
        {
            sentence += " The gap was already there before the extra.";
        }

        return sentence;
    }

    /// <summary>
    /// The wording that may be said. Every amount and date in it comes from a copied figure or a tradeoff that used one.
    /// </summary>
    private static string Statement(
        IReadOnlyList<PlanningForecastFigure> figures,
        PlanningForecastContext context,
        IReadOnlyList<PlanningForecastTradeoff> tradeoffs)
    {
        var currency = Currency(context.PlanningCurrency);
        var sentences = new List<string>
        {
            "These amounts and dates are estimates from the plan calculation. They stay tied to the assumptions included with them."
        };
        sentences.Add(context.HasDebts
            ? StartingMinimums(figures, currency)
            : "No debt is recorded. There is no payoff date to estimate.");
        if (context.HasDebts)
        {
            foreach (var path in Paths)
            {
                sentences.Add(PathSentence(figures, path, currency));
            }
        }

        sentences.Add(CashSentence(figures, currency));
        sentences.Add(LowPaySentence(figures, currency));
        foreach (var path in Paths)
        {
            sentences.Add(SpendableSentence(figures, path, currency));
        }

        sentences.Add(SpendingSentence(figures, currency));
        sentences.Add(FloorSentence(figures, currency));
        foreach (var uncertainty in Distinct(context.Uncertainties))
        {
            sentences.Add(UncertaintySentence(uncertainty));
        }

        foreach (var tradeoff in tradeoffs)
        {
            sentences.Add(tradeoff.Explanation);
        }

        if (context.MissingBalanceNames.Count > 0)
        {
            sentences.Add(
                string.Join(", ", context.MissingBalanceNames)
                + " has no balance and is left out of the payoff. A missing balance is not zero.");
        }

        if (context.ExcludedCurrencies.Count > 0)
        {
            sentences.Add(CurrencyRule(context, currency));
        }

        return string.Join(" ", sentences.Where(sentence => !string.IsNullOrWhiteSpace(sentence)));
    }

    /// <summary>
    /// The known minimums before any payoff. Missing minimums stay unknown.
    /// </summary>
    private static string StartingMinimums(IReadOnlyList<PlanningForecastFigure> figures, string currency)
    {
        var starting = AmountOf(
            figures,
            PlanningForecastFigureKind.StartingObligation,
            PayoffRolloverKind.Rollover);
        return starting is decimal amount
            ? "Known minimums start at " + Money(amount, currency) + "."
            : "Known minimums are unknown.";
    }

    /// <summary>
    /// One freed-cash path. A missing payoff date is not projected, and a missing remaining minimum is unknown.
    /// </summary>
    private static string PathSentence(
        IReadOnlyList<PlanningForecastFigure> figures,
        PayoffRolloverKind path,
        string currency)
    {
        var paid = DateOf(figures, PlanningForecastFigureKind.PaidOffOn, path);
        var interest = AmountOf(figures, PlanningForecastFigureKind.TotalInterest, path);
        var room = AmountOf(figures, PlanningForecastFigureKind.RecurringRoom, path);
        var kept = AmountOf(figures, PlanningForecastFigureKind.CashReclaimed, path);
        var rolled = AmountOf(figures, PlanningForecastFigureKind.CashRolled, path);
        var remaining = AmountOf(figures, PlanningForecastFigureKind.RemainingObligation, path);
        if (interest is null && room is null && kept is null && rolled is null && paid is null)
        {
            return "";
        }

        return PathName(path)
            + ": the estimated payoff date is "
            + (paid is DateOnly date ? When(date) : "not projected")
            + ". Interest is "
            + (interest is decimal interestAmount ? Money(interestAmount, currency) : "not calculated")
            + ". Breathing room after the last change is "
            + (room is decimal roomAmount ? Money(roomAmount, currency) : "not calculated")
            + " a month. Freed cash kept is "
            + (kept is decimal keptAmount ? Money(keptAmount, currency) : "not calculated")
            + ". Freed cash sent to a later debt is "
            + (rolled is decimal rolledAmount ? Money(rolledAmount, currency) : "not calculated")
            + ". Known minimums still due are "
            + (remaining is decimal remainingAmount ? Money(remainingAmount, currency) : "unknown")
            + ".";
    }

    /// <summary>
    /// Starting cash and the keeping-none windows. A window the calculation did not return is skipped.
    /// </summary>
    private static string CashSentence(IReadOnlyList<PlanningForecastFigure> figures, string currency)
    {
        var sentences = new List<string>();
        var starting = AmountOf(figures, PlanningForecastFigureKind.StartingCash);
        if (starting is decimal cash)
        {
            sentences.Add("Starting cash is " + Money(cash, currency) + ".");
        }

        foreach (var horizon in Windows)
        {
            var ending = AmountOf(
                figures,
                PlanningForecastFigureKind.EndingCash,
                PayoffRolloverKind.Rollover,
                basis: PlanningForecastBasis.Typical,
                horizonMonths: horizon);
            var lowest = AmountOf(
                figures,
                PlanningForecastFigureKind.LowestCash,
                PayoffRolloverKind.Rollover,
                basis: PlanningForecastBasis.Typical,
                horizonMonths: horizon);
            var lowestOn = DateOf(
                figures,
                PlanningForecastFigureKind.LowestCash,
                PayoffRolloverKind.Rollover,
                basis: PlanningForecastBasis.Typical,
                horizonMonths: horizon);
            if (ending is null && lowest is null)
            {
                continue;
            }

            var sentence = "Keeping none, over " + Horizon(horizon) + ", ending cash is "
                + (ending is decimal endingAmount ? Money(endingAmount, currency) : "not calculated")
                + " and the lowest cash is "
                + (lowest is decimal lowestAmount ? Money(lowestAmount, currency) : "not calculated");
            if (lowestOn is DateOnly on)
            {
                sentence += " on " + When(on);
            }

            sentences.Add(sentence + ".");
            var unknown = CountOf(
                figures,
                PlanningForecastFigureKind.UnknownMinimumCount,
                PayoffRolloverKind.Rollover,
                basis: PlanningForecastBasis.Typical,
                horizonMonths: horizon);
            if (unknown is int count && count > 0)
            {
                var word = count == 1 ? "minimum is" : "minimums are";
                sentences.Add(count + " remaining " + word + " unknown.");
            }
        }

        return string.Join(" ", sentences);
    }

    /// <summary>
    /// The 30-day low at low pay, when the calculation produced one.
    /// </summary>
    private static string LowPaySentence(IReadOnlyList<PlanningForecastFigure> figures, string currency)
    {
        var lowest = AmountOf(
            figures,
            PlanningForecastFigureKind.LowestCash,
            PayoffRolloverKind.Rollover,
            basis: PlanningForecastBasis.LowPay,
            horizonMonths: 0);
        return lowest is decimal amount
            ? "At low pay, keeping none, the lowest cash over 30 days is " + Money(amount, currency) + "."
            : "";
    }

    /// <summary>
    /// Spendable cash for one path. It is the dated cash left, not a monthly surplus.
    /// </summary>
    private static string SpendableSentence(
        IReadOnlyList<PlanningForecastFigure> figures,
        PayoffRolloverKind path,
        string currency)
    {
        var spendable = AmountOf(figures, PlanningForecastFigureKind.Spendable, path);
        var lowest = AmountOf(figures, PlanningForecastFigureKind.LowestAvailable, path);
        if (spendable is null && lowest is null)
        {
            return "";
        }

        var sentence = PathName(path)
            + ": spendable cash over 30 days is "
            + (spendable is decimal spendableAmount ? Money(spendableAmount, currency) : "not calculated")
            + ". The lowest cash left after set-aside money is "
            + (lowest is decimal lowestAmount ? Money(lowestAmount, currency) : "not calculated");
        var on = DateOf(figures, PlanningForecastFigureKind.LowestAvailable, path);
        if (on is DateOnly date)
        {
            sentence += " on " + When(date);
        }

        sentence += ".";
        sentence = AppendAmount(
            sentence,
            figures,
            PlanningForecastFigureKind.ReliableIncome,
            path,
            " Steady pay in that window is ",
            currency);
        sentence = AppendAmount(
            sentence,
            figures,
            PlanningForecastFigureKind.SpendableObligations,
            path,
            " Dated bills and debt payments are ",
            currency);
        sentence = AppendAmount(
            sentence,
            figures,
            PlanningForecastFigureKind.OrdinarySpending,
            path,
            " Flexible spending inside 30 days is ",
            currency);
        sentence = AppendAmount(
            sentence,
            figures,
            PlanningForecastFigureKind.Reserved,
            path,
            " Set-aside money is ",
            currency);
        return sentence;
    }

    /// <summary>
    /// Adds one amount sentence when the calculation returned it.
    /// </summary>
    private static string AppendAmount(
        string sentence,
        IReadOnlyList<PlanningForecastFigure> figures,
        PlanningForecastFigureKind kind,
        PayoffRolloverKind path,
        string lead,
        string currency)
    {
        var amount = AmountOf(figures, kind, path);
        return amount is decimal value
            ? sentence + lead + Money(value, currency) + "."
            : sentence;
    }

    /// <summary>
    /// The flexible spending the calculation used. A blank monthly amount stays unrecorded.
    /// </summary>
    private static string SpendingSentence(IReadOnlyList<PlanningForecastFigure> figures, string currency)
    {
        var scenario = AmountOf(figures, PlanningForecastFigureKind.FlexibleSpending);
        if (scenario is decimal flexible)
        {
            return "This estimate uses "
                + Money(flexible, currency)
                + " a month for flexible spending. That spending stays in the estimate.";
        }

        var saved = AmountOf(figures, PlanningForecastFigureKind.LivingSpending);
        return saved is decimal amount && amount > 0
            ? "Flexible spending counted here is " + Money(amount, currency) + " a month."
            : "No monthly flexible spending is recorded.";
    }

    /// <summary>
    /// The cash floor the calculation protected. Zero means no floor was saved.
    /// </summary>
    private static string FloorSentence(IReadOnlyList<PlanningForecastFigure> figures, string currency)
    {
        var scenario = AmountOf(figures, PlanningForecastFigureKind.CashToKeep);
        if (scenario is decimal floor)
        {
            return "This estimate protects " + Money(floor, currency) + ".";
        }

        var saved = AmountOf(figures, PlanningForecastFigureKind.SavedCashFloor);
        return saved is decimal amount && amount > 0
            ? "Cash to keep is " + Money(amount, currency) + "."
            : "No cash floor is saved.";
    }

    /// <summary>
    /// One copy of each spendable omission. The same omission on every freed-cash choice is said once.
    /// A missing flexible-spending amount is already said with the spending sentence.
    /// </summary>
    private static IEnumerable<PlanningForecastUncertainty> Distinct(
        IReadOnlyList<PlanningForecastUncertainty> uncertainties)
    {
        var seen = new HashSet<(SpendableUncertaintyKind Kind, string Name)>();
        foreach (var uncertainty in uncertainties)
        {
            if (uncertainty.Kind == SpendableUncertaintyKind.OrdinarySpendingUnset)
            {
                continue;
            }

            if (seen.Add((uncertainty.Kind, uncertainty.Name ?? "")))
            {
                yield return uncertainty;
            }
        }
    }

    /// <summary>
    /// A spendable omission, in the calculation's own terms.
    /// </summary>
    private static string UncertaintySentence(PlanningForecastUncertainty uncertainty)
    {
        var named = string.IsNullOrWhiteSpace(uncertainty.Name) ? "" : " " + uncertainty.Name.Trim();
        return uncertainty.Kind switch
        {
            SpendableUncertaintyKind.VariableIncome => "Variable pay" + named + " is left out of spendable cash.",
            SpendableUncertaintyKind.UncertainIncome => "Uncertain pay" + named + " is left out of spendable cash.",
            SpendableUncertaintyKind.NoReliableIncome =>
                "No steady pay is recorded, so spendable cash does not assume a paycheck.",
            SpendableUncertaintyKind.WindfallLeftOut => "The windfall is left out of spendable cash.",
            SpendableUncertaintyKind.IncomeLeftOut => "Some pay" + named + " is left out of spendable cash.",
            SpendableUncertaintyKind.ExcludedCurrency => "Another currency" + named + " is left out.",
            SpendableUncertaintyKind.DebtLeftOut => "A debt" + named + " is left out of spendable cash.",
            _ => ""
        };
    }

    /// <summary>
    /// Rejects wording that promises a result, adds a term, ranks one side as automatically better, or shames.
    /// </summary>
    private static string? RejectLanguage(string claim)
    {
        foreach (var (needle, error, word) in Banned)
        {
            if (word ? ContainsWord(claim, needle) : claim.Contains(needle, StringComparison.OrdinalIgnoreCase))
            {
                return error;
            }
        }

        return null;
    }

    /// <summary>
    /// True when the word appears on its own, so a longer word is not flagged.
    /// </summary>
    private static bool ContainsWord(string text, string word)
    {
        var index = 0;
        while (index < text.Length)
        {
            var found = text.IndexOf(word, index, StringComparison.OrdinalIgnoreCase);
            if (found < 0)
            {
                return false;
            }

            var before = found == 0 || !char.IsLetter(text[found - 1]);
            var after = found + word.Length >= text.Length || !char.IsLetter(text[found + word.Length]);
            if (before && after)
            {
                return true;
            }

            index = found + word.Length;
        }

        return false;
    }

    /// <summary>
    /// The debt and income names that may appear in the wording.
    /// A digit in one of these names is not treated as a forecast amount.
    /// </summary>
    private static IReadOnlyList<string> Names(
        IReadOnlyList<PlanningForecastFigure> figures,
        PlanningForecastContext context)
    {
        return figures
            .Select(figure => figure.Name)
            .Concat(context.MissingBalanceNames)
            .Concat(context.Uncertainties.Select(uncertainty => uncertainty.Name))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!.Trim())
            .Where(name => name.Any(char.IsLetter))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Removes copied names before the number check, so a digit in a name is left alone.
    /// A name that is only digits is not removed.
    /// </summary>
    private static string WithoutNames(string claim, IReadOnlyList<string> names)
    {
        var text = claim;
        foreach (var name in names.OrderByDescending(name => name.Length))
        {
            text = text.Replace(name, " ", StringComparison.Ordinal);
        }

        return text;
    }

    /// <summary>
    /// Rejects a date, a money amount, or another number that is not among the copied figures.
    /// </summary>
    private static string? RejectNumbers(string claim, IReadOnlyList<PlanningForecastFigure> figures)
    {
        var text = CommaPattern.Replace(claim, "$1");
        foreach (Match date in DatePattern.Matches(text))
        {
            if (!DateOnly.TryParseExact(
                    date.Value,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed)
                || !figures.Any(figure => figure.On == parsed))
            {
                return "That date is not in the calculation.";
            }
        }

        text = DatePattern.Replace(text, " ");
        foreach (Match money in MoneyPattern.Matches(text))
        {
            if (!decimal.TryParse(money.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                || !figures.Any(figure => figure.Amount is decimal amount && Cents(amount) == Cents(parsed)))
            {
                return "That amount is not in the calculation.";
            }
        }

        text = MoneyPattern.Replace(text, " ");
        foreach (Match number in IntegerPattern.Matches(text))
        {
            if (!int.TryParse(number.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
                || !AllowedCount(parsed, text, number.Index, figures))
            {
                return "That number is not in the calculation.";
            }
        }

        return null;
    }

    /// <summary>
    /// True when the integer is a copied count or whole amount, or one of the horizon and year-limit labels.
    /// A percent after a horizon label is not that label.
    /// </summary>
    private static bool AllowedCount(
        int number,
        string text,
        int index,
        IReadOnlyList<PlanningForecastFigure> figures)
    {
        if (number is 6 or 12 or 18 or 30 or 50
            && !FollowedByPercent(text, index, number.ToString(CultureInfo.InvariantCulture).Length))
        {
            return true;
        }

        return figures.Any(figure =>
            figure.Count == number
            || figure.HorizonMonths == number
            || (figure.Amount is decimal amount && amount == number));
    }

    /// <summary>
    /// True when a percent sign or the word percent follows this number.
    /// </summary>
    private static bool FollowedByPercent(string text, int index, int length)
    {
        var rest = text[(index + length)..].TrimStart();
        return rest.StartsWith('%') || rest.StartsWith("percent", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The copied amount for one figure, when the calculation supplied it.
    /// </summary>
    private static decimal? AmountOf(
        IReadOnlyList<PlanningForecastFigure> figures,
        PlanningForecastFigureKind kind,
        PayoffRolloverKind? path = null,
        PayoffPriorityChoice? priority = null,
        PlanningForecastBasis basis = PlanningForecastBasis.Typical,
        int? horizonMonths = null)
    {
        return Find(figures, kind, path, priority, basis, horizonMonths)?.Amount;
    }

    /// <summary>
    /// The copied date for one figure, when the calculation supplied it.
    /// </summary>
    private static DateOnly? DateOf(
        IReadOnlyList<PlanningForecastFigure> figures,
        PlanningForecastFigureKind kind,
        PayoffRolloverKind? path = null,
        PayoffPriorityChoice? priority = null,
        PlanningForecastBasis basis = PlanningForecastBasis.Typical,
        int? horizonMonths = null)
    {
        return Find(figures, kind, path, priority, basis, horizonMonths)?.On;
    }

    /// <summary>
    /// The copied count for one figure, when the calculation supplied it.
    /// </summary>
    private static int? CountOf(
        IReadOnlyList<PlanningForecastFigure> figures,
        PlanningForecastFigureKind kind,
        PayoffRolloverKind? path = null,
        PayoffPriorityChoice? priority = null,
        PlanningForecastBasis basis = PlanningForecastBasis.Typical,
        int? horizonMonths = null)
    {
        return Find(figures, kind, path, priority, basis, horizonMonths)?.Count;
    }

    /// <summary>
    /// The first copied figure with this identity.
    /// </summary>
    private static PlanningForecastFigure? Find(
        IReadOnlyList<PlanningForecastFigure> figures,
        PlanningForecastFigureKind kind,
        PayoffRolloverKind? path,
        PayoffPriorityChoice? priority,
        PlanningForecastBasis basis,
        int? horizonMonths)
    {
        return figures.FirstOrDefault(figure =>
            figure.Kind == kind
            && figure.Path == path
            && figure.Priority == priority
            && figure.Basis == basis
            && figure.HorizonMonths == horizonMonths);
    }

    /// <summary>
    /// Cents, so a copied amount matches the same cents in a claim.
    /// </summary>
    private static decimal Cents(decimal amount)
    {
        return decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Cents and the planning currency, in a fixed form a claim can be checked against.
    /// </summary>
    private static string Money(decimal amount, string currency)
    {
        return Cents(amount).ToString("0.00", CultureInfo.InvariantCulture) + " " + currency;
    }

    /// <summary>
    /// A date in a fixed numeric form, so the claim check does not depend on the machine's language.
    /// </summary>
    private static string When(DateOnly date)
    {
        return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The planning currency code the calculation counted.
    /// A blank code is left blank. This does not substitute a code.
    /// </summary>
    private static string Currency(string? currency)
    {
        return string.IsNullOrWhiteSpace(currency)
            ? ""
            : currency.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// The freed-cash choice in the words the forecast uses.
    /// </summary>
    private static string PathName(PayoffRolloverKind path)
    {
        return path switch
        {
            PayoffRolloverKind.Reclaim => "Keeping some",
            PayoffRolloverKind.ReclaimAll => "Keeping all",
            _ => "Keeping none"
        };
    }

    /// <summary>
    /// The payoff order in the words the forecast uses.
    /// </summary>
    private static string PriorityName(PayoffPriorityChoice priority)
    {
        return priority switch
        {
            PayoffPriorityChoice.MinimumRelease => "removing a minimum",
            PayoffPriorityChoice.Utilization => "lowering utilization",
            PayoffPriorityChoice.Constraint => "a chosen debt first or last",
            _ => "highest rate first"
        };
    }

    /// <summary>
    /// The 30-day window, or a 6, 12, or 18 month horizon.
    /// </summary>
    private static string Horizon(int months)
    {
        return months == 0 ? "30 days" : months + " months";
    }

    #endregion
}
