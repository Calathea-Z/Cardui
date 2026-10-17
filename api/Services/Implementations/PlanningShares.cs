using System.Globalization;
using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Conversations;
using Cardui.Api.Domain.Debts;
using Cardui.Api.Domain.Planning;
using Cardui.Api.Domain.Plans;
using Cardui.Api.Dtos.Allocation;
using Cardui.Api.Dtos.Conversations;
using Cardui.Api.Dtos.Debts;
using Cardui.Api.Dtos.Income;
using Cardui.Api.Dtos.Living;
using Cardui.Api.Dtos.Obligations;
using Cardui.Api.Dtos.Plan;
using Cardui.Api.Dtos.Planning;
using Cardui.Api.Dtos.Savings;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;

namespace Cardui.Api.Services.Implementations;

public class PlanningShares : IPlanningShares
{
    private readonly IConversationService _conversations;
    private readonly IPlanningTools _tools;
    private readonly IDebtsService _debts;
    private readonly IPlanningFacts _facts;
    private readonly IPlanningForecasts _forecasts;
    private readonly HouseholdScope _householdScope;

    public PlanningShares(
        IConversationService conversations,
        IPlanningTools tools,
        IDebtsService debts,
        IPlanningFacts facts,
        IPlanningForecasts forecasts,
        HouseholdScope householdScope)
    {
        _conversations = conversations;
        _tools = tools;
        _debts = debts;
        _facts = facts;
        _forecasts = forecasts;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<PlanningShareConsentDto> GetConsentAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        RequireHousehold();
        var conversation = await _conversations.GetAsync(conversationId, cancellationToken);
        return ConsentDto(conversation);
    }

    /// <inheritdoc />
    public async Task<PlanningShareConsentDto> AllowAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        RequireHousehold();
        var conversation = await _conversations.SetDataSharingAsync(
            conversationId,
            true,
            cancellationToken);
        return ConsentDto(conversation);
    }

    /// <inheritdoc />
    public async Task<PlanningShareConsentDto> DeclineAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        RequireHousehold();
        var conversation = await _conversations.SetDataSharingAsync(
            conversationId,
            false,
            cancellationToken);
        return ConsentDto(conversation);
    }

    /// <inheritdoc />
    public async Task<PlanningShareDto> PrepareAsync(
        Guid conversationId,
        PlanningShareRequest request,
        CancellationToken cancellationToken = default)
    {
        RequireHousehold();
        var conversation = await _conversations.GetAsync(conversationId, cancellationToken);
        Reject(ConversationRules.RejectClosed(conversation.Status));
        var consent = ConsentOf(conversation);
        if (consent != PlanningShareConsent.Allowed)
        {
            return ToDto(PlanningShareRules.Compose(consent, null, [], null));
        }

        request ??= new PlanningShareRequest();
        if (PlanningShareRules.ContainsToken(request.Message))
        {
            return ToDto(PlanningShareRules.Compose(consent, request.Message, [], null));
        }

        var sections = request.Sections ?? [];
        Reject(PlanningShareRules.RejectMessage(request.Message));
        Reject(PlanningShareRules.RejectSections(sections));
        Reject(PlanningShareRules.RejectUntrusted(request.UntrustedText, request.UntrustedKind));
        var untrusted = MapUntrusted(request);
        if (string.IsNullOrWhiteSpace(request.Message)
            && sections.Count == 0
            && untrusted is null)
        {
            throw new BadRequestException(PlanningShareRules.NothingToShare);
        }

        var parts = await LoadPartsAsync(conversation.Summary, sections, cancellationToken);
        return ToDto(PlanningShareRules.Compose(consent, request.Message, parts, untrusted));
    }

    #region Private Methods

    /// <summary>
    /// Requires a household before a conversation is read.
    /// </summary>
    private void RequireHousehold()
    {
        _householdScope.RequireHouseholdId();
    }

    /// <summary>
    /// Throws when a share request breaks a sharing rule.
    /// </summary>
    private static void Reject(string? error)
    {
        if (error is not null)
        {
            throw new BadRequestException(error);
        }
    }

    /// <summary>
    /// Reads the sharing choice from the reserved note. Any other note is ignored.
    /// </summary>
    private static PlanningShareConsent ConsentOf(ConversationResumeDto conversation)
    {
        var choice = conversation.Preferences.FirstOrDefault(preference =>
            ConversationRules.SameText(preference.Key, PlanningShareRules.ConsentKey));
        return PlanningShareRules.ParseConsent(choice?.Value);
    }

    /// <summary>
    /// Copies the sharing choice into the API shape.
    /// </summary>
    private static PlanningShareConsentDto ConsentDto(ConversationResumeDto conversation)
    {
        return new PlanningShareConsentDto
        {
            ConversationId = conversation.Id,
            Consent = ConsentOf(conversation)
        };
    }

    /// <summary>
    /// Copies a prepared share into the API shape. A refusal has no model text.
    /// </summary>
    private static PlanningShareDto ToDto(PlanningShare share)
    {
        return new PlanningShareDto
        {
            Consent = share.Consent,
            Sendable = share.Sendable,
            Input = share.Input,
            Instructions = share.Instructions,
            Included = share.Included,
            Omissions = share.Omissions
        };
    }

    /// <summary>
    /// Returns the excerpt when one was supplied. A blank excerpt is absent.
    /// </summary>
    private static PlanningUntrustedText? MapUntrusted(PlanningShareRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UntrustedText) || request.UntrustedKind is null)
        {
            return null;
        }

        return new PlanningUntrustedText(request.UntrustedKind.Value, request.UntrustedText);
    }

    /// <summary>
    /// Loads only the named sections. Transactions, import files, and bank tokens are not read.
    /// </summary>
    private async Task<List<PlanningSharePart>> LoadPartsAsync(
        string? summary,
        IReadOnlyList<PlanningShareSection> sections,
        CancellationToken cancellationToken)
    {
        var selected = sections.ToHashSet();
        var parts = new List<PlanningSharePart>();
        if (selected.Contains(PlanningShareSection.Summary))
        {
            parts.AddRange(Lines(
                PlanningShareSection.Summary,
                string.IsNullOrWhiteSpace(summary) ? "No summary is saved." : summary));
        }

        if (selected.Contains(PlanningShareSection.Cash))
        {
            var cash = await _tools.ReadSummaryAsync(PlanningSummaryKind.Cash, cancellationToken);
            parts.AddRange(CashLines(Required(cash.Cash, "Cash")));
        }

        if (selected.Contains(PlanningShareSection.Debts))
        {
            parts.AddRange(DebtLines(await _debts.GetDebtsAsync(cancellationToken)));
        }

        if (selected.Contains(PlanningShareSection.Income))
        {
            var income = await _tools.ReadSummaryAsync(PlanningSummaryKind.Income, cancellationToken);
            parts.AddRange(IncomeLines(income.Income ?? []));
        }

        if (selected.Contains(PlanningShareSection.Bills))
        {
            var bills = await _tools.ReadSummaryAsync(PlanningSummaryKind.Bills, cancellationToken);
            parts.AddRange(BillLines(bills.Bills ?? []));
        }

        if (selected.Contains(PlanningShareSection.Savings))
        {
            var savings = await _tools.ReadSummaryAsync(PlanningSummaryKind.Savings, cancellationToken);
            parts.AddRange(SavingsLines(savings.Savings ?? []));
        }

        if (selected.Contains(PlanningShareSection.Living))
        {
            var living = await _tools.ReadSummaryAsync(PlanningSummaryKind.Living, cancellationToken);
            parts.AddRange(LivingLines(Required(living.Living, "Living")));
        }

        if (selected.Contains(PlanningShareSection.Allocation))
        {
            var allocation = await _tools.ReadSummaryAsync(
                PlanningSummaryKind.Allocation,
                cancellationToken);
            parts.AddRange(AllocationLines(Required(allocation.Allocation, "Allocation")));
        }

        if (selected.Contains(PlanningShareSection.Plan))
        {
            var plan = await _tools.ReadSummaryAsync(PlanningSummaryKind.Plan, cancellationToken);
            parts.AddRange(PlanLines(plan.Plan ?? []));
        }

        if (selected.Contains(PlanningShareSection.Facts))
        {
            parts.AddRange(FactLines(await _facts.DescribeAsync(cancellationToken)));
        }

        if (selected.Contains(PlanningShareSection.Forecast))
        {
            parts.AddRange(ForecastLines(
                await _forecasts.DescribeAsync(new PlanScenarioQuery(), cancellationToken)));
        }

        return parts;
    }

    /// <summary>
    /// Requires a summary the tool always sets for that section.
    /// </summary>
    private static T Required<T>(T? value, string name)
        where T : class
    {
        return value ?? throw new InvalidOperationException(name + " summary was not returned.");
    }

    /// <summary>
    /// Splits text into share lines. A blank line is skipped.
    /// </summary>
    private static List<PlanningSharePart> Lines(PlanningShareSection section, string text)
    {
        var parts = new List<PlanningSharePart>();
        foreach (var line in text.Split('\n'))
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                parts.Add(new PlanningSharePart(section, line.Trim()));
            }
        }

        return parts;
    }

    /// <summary>
    /// Describes cash on hand without account names, masks, or connection tokens.
    /// The currency is the one bound for this request.
    /// </summary>
    private List<PlanningSharePart> CashLines(CashPosition cash)
    {
        var currency = _householdScope.PlanningCurrency;
        var lines = new List<string>
        {
            "Cash on hand is "
            + Money(cash.Total, currency)
            + " across "
            + Count(cash.AccountCount, "account", "accounts")
            + "."
        };
        if (cash.ConnectedAccountCount > 0)
        {
            lines.Add(Count(cash.ConnectedAccountCount, "account is connected", "accounts are connected") + ".");
        }

        if (cash.OldestBalanceAsOf is DateOnly date)
        {
            lines.Add("The oldest balance is " + date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".");
        }

        if (cash.StaleConnectedAccountCount > 0)
        {
            lines.Add(
                Count(cash.StaleConnectedAccountCount, "connected balance is stale", "connected balances are stale")
                + ".");
        }

        if (cash.UnknownBalanceDateCount > 0)
        {
            lines.Add(
                Count(cash.UnknownBalanceDateCount, "balance date is unknown", "balance dates are unknown")
                + ".");
        }

        return lines.Select(line => new PlanningSharePart(PlanningShareSection.Cash, line)).ToList();
    }

    /// <summary>
    /// Names each debt's plan balance and terms. Account ids, masks, and synced copies are left out.
    /// </summary>
    private static List<PlanningSharePart> DebtLines(IReadOnlyList<DebtDto> debts)
    {
        if (debts.Count == 0)
        {
            return Lines(PlanningShareSection.Debts, "No debt is recorded.");
        }

        return debts.Select(debt => new PlanningSharePart(PlanningShareSection.Debts, DebtLine(debt))).ToList();
    }

    /// <summary>
    /// One debt in a single line. A null term is omitted, so an unknown minimum is not zero.
    /// </summary>
    private static string DebtLine(DebtDto debt)
    {
        var parts = new List<string>
        {
            debt.BalanceInUse is decimal balance
                ? "balance " + Money(balance, debt.Currency)
                : "balance unknown"
        };
        if (debt.Apr is decimal apr)
        {
            parts.Add("rate " + apr.ToString("0.##", CultureInfo.InvariantCulture) + " percent");
        }

        if (debt.MinimumPayment is decimal minimum)
        {
            parts.Add("minimum " + Money(minimum, debt.Currency));
        }

        if (debt.NextDueDate is DateOnly due)
        {
            parts.Add("due " + due.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        if (debt.PromotionalEndsOn is DateOnly promo)
        {
            parts.Add("promotion ends " + promo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        if (debt.Freshness is DebtLinkFreshness freshness && freshness != DebtLinkFreshness.Current)
        {
            parts.Add("connection is not current");
        }

        return debt.Name.Trim() + ": " + string.Join(", ", parts) + ".";
    }

    /// <summary>
    /// Names each paycheck's take-home, cadence, and next date. Strong pay, gross pay, and later dates stay off the share.
    /// </summary>
    private static List<PlanningSharePart> IncomeLines(IReadOnlyList<IncomeSourceDto> income)
    {
        if (income.Count == 0)
        {
            return Lines(PlanningShareSection.Income, "No paycheck is recorded.");
        }

        return income.Select(source => new PlanningSharePart(
            PlanningShareSection.Income,
            IncomeLine(source))).ToList();
    }

    /// <summary>
    /// One paycheck. Low pay is included when the person recorded it.
    /// </summary>
    private static string IncomeLine(IncomeSourceDto source)
    {
        var line = source.Name.Trim()
            + ": "
            + Money(source.TakeHomeAmount, source.Currency)
            + " "
            + Cadence(source.Cadence)
            + ", next "
            + source.NextPaymentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(source.ContributorName))
        {
            line += ", for " + source.ContributorName.Trim();
        }

        if (source.LowTakeHomeAmount is decimal low)
        {
            line += ", low pay " + Money(low, source.Currency);
        }

        return line + ".";
    }

    /// <summary>
    /// Names each saved bill. The source account id is left out.
    /// </summary>
    private static List<PlanningSharePart> BillLines(IReadOnlyList<ObligationDto> bills)
    {
        if (bills.Count == 0)
        {
            return Lines(PlanningShareSection.Bills, "No bill is saved.");
        }

        return bills.Select(bill => new PlanningSharePart(PlanningShareSection.Bills, BillLine(bill))).ToList();
    }

    /// <summary>
    /// One saved bill. Essential and flexible stay visible.
    /// </summary>
    private static string BillLine(ObligationDto bill)
    {
        var flexibility = bill.Flexibility == ObligationFlexibility.Essential ? "essential" : "flexible";
        return bill.Name.Trim()
            + ": "
            + Money(bill.Amount, bill.Currency)
            + " "
            + Cadence(bill.Cadence)
            + ", due "
            + bill.NextDueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            + ", "
            + flexibility
            + ".";
    }

    /// <summary>
    /// Names each savings row. The account mask and account id are left out.
    /// </summary>
    private static List<PlanningSharePart> SavingsLines(IReadOnlyList<SavingsGoalDto> goals)
    {
        if (goals.Count == 0)
        {
            return Lines(PlanningShareSection.Savings, "No savings row is saved.");
        }

        return goals.Select(goal => new PlanningSharePart(PlanningShareSection.Savings, SavingsLine(goal))).ToList();
    }

    /// <summary>
    /// One savings row. A followed balance uses the amount in use and does not name the account number.
    /// </summary>
    private static string SavingsLine(SavingsGoalDto goal)
    {
        return goal.Kind switch
        {
            SavingsGoalKind.Operating => goal.MonthlyAmount is decimal monthly
                ? "Monthly living spending is " + Money(monthly, goal.Currency) + "."
                : "Monthly living spending is not recorded.",
            SavingsGoalKind.Floor => "Cash to keep is "
                + Money(goal.FloorAmount ?? goal.AmountInUse, goal.Currency)
                + ".",
            _ => NamedSavings(goal)
        };
    }

    /// <summary>
    /// A named savings goal. The target is included when one is saved.
    /// </summary>
    private static string NamedSavings(SavingsGoalDto goal)
    {
        var line = goal.Name.Trim() + ": " + Money(goal.AmountInUse, goal.Currency) + " set aside";
        if (goal.TargetAmount is decimal target)
        {
            line += ", target " + Money(target, goal.Currency);
            if (goal.TargetDate is DateOnly date)
            {
                line += " by " + date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
        }

        return line + ".";
    }

    /// <summary>
    /// Describes shared pay and the monthly gap. The cash-account list, including masks, is left out.
    /// </summary>
    private static List<PlanningSharePart> LivingLines(LivingPageDto living)
    {
        var currency = living.PlanningCurrency;
        var gap = living.Gap;
        var lines = new List<string>
        {
            "Shared pay is " + Money(gap.SharedMonthly, currency) + ".",
            "Bills average " + Money(gap.BillsMonthly, currency) + ".",
            "Minimums average " + Money(gap.MinimumsMonthly, currency) + ".",
            "Monthly living spending is " + Money(gap.LivingSpendingMonthly, currency) + ".",
            "Shortfall is " + Money(gap.Shortfall, currency) + "."
        };
        foreach (var contribution in living.Contributions)
        {
            lines.Add(contribution.Name.Trim() + " shares " + Money(contribution.SharedMonthly, currency) + ".");
        }

        if (gap.LeftOut is { Count: > 0 })
        {
            lines.Add("Left out: " + string.Join(", ", gap.LeftOut) + ".");
        }

        return lines.Select(line => new PlanningSharePart(PlanningShareSection.Living, line)).ToList();
    }

    /// <summary>
    /// States the saved split. Review amounts and source names are left out.
    /// </summary>
    private static List<PlanningSharePart> AllocationLines(AllocationPageDto page)
    {
        if (!page.IsSaved)
        {
            return Lines(PlanningShareSection.Allocation, "No allocation is saved.");
        }

        return
        [
            new PlanningSharePart(PlanningShareSection.Allocation, Split("Variable income", page.VariableIncome)),
            new PlanningSharePart(PlanningShareSection.Allocation, Split("A windfall", page.Windfall))
        ];
    }

    /// <summary>
    /// One saved percent split. The remainder stays available.
    /// </summary>
    private static string Split(string name, AllocationRuleDto rule)
    {
        return name
            + " is "
            + rule.DebtPercent
            + " percent to debt, "
            + rule.SavingsPercent
            + " percent to savings, and "
            + rule.CashPercent
            + " percent to cash.";
    }

    /// <summary>
    /// Names each saved plan and the actions on a draft or accepted plan. A superseded plan is only its status.
    /// </summary>
    private static List<PlanningSharePart> PlanLines(IReadOnlyList<PlanVersionDto> versions)
    {
        if (versions.Count == 0)
        {
            return Lines(PlanningShareSection.Plan, "No plan is saved.");
        }

        var lines = new List<string>();
        foreach (var version in versions)
        {
            lines.Add("Plan " + version.Number + " is " + PlanStatus(version.Status) + ".");
            if (version.Status == PlanVersionStatus.Superseded)
            {
                continue;
            }

            foreach (var action in version.Plan.Actions)
            {
                lines.Add(ActionLine(action, version.Plan.PlanningCurrency));
            }
        }

        return lines.Select(line => new PlanningSharePart(PlanningShareSection.Plan, line)).ToList();
    }

    /// <summary>
    /// The status words used in a share.
    /// </summary>
    private static string PlanStatus(PlanVersionStatus status)
    {
        return status switch
        {
            PlanVersionStatus.Draft => "a draft",
            PlanVersionStatus.Accepted => "accepted",
            _ => "superseded"
        };
    }

    /// <summary>
    /// One ranked action. The amount is included when the plan captured one.
    /// </summary>
    private static string ActionLine(DecisionPlanActionDto action, string currency)
    {
        var line = action.Order + ". " + ActionWords(action.Kind);
        if (!string.IsNullOrWhiteSpace(action.Name))
        {
            line += " for " + action.Name.Trim();
        }

        if (action.Amount is decimal amount)
        {
            line += ", " + Money(amount, currency);
        }

        return line + ".";
    }

    /// <summary>
    /// Plain words for one decision-plan action.
    /// </summary>
    private static string ActionWords(DecisionPlanActionKind kind)
    {
        return kind switch
        {
            DecisionPlanActionKind.CoverCashShortfall => "Cover the cash shortfall",
            DecisionPlanActionKind.LowerExtra => "Lower the extra payment",
            DecisionPlanActionKind.AddStartingCash => "Add starting cash",
            DecisionPlanActionKind.AddDebt => "Add a debt",
            DecisionPlanActionKind.AddIncome => "Add income",
            DecisionPlanActionKind.FinishDebtTerms => "Finish the debt terms",
            DecisionPlanActionKind.RefreshBalances => "Refresh balances",
            DecisionPlanActionKind.HoldIncomeLoss => "Hold the income-loss preview",
            DecisionPlanActionKind.HoldWindfall => "Hold the windfall preview",
            DecisionPlanActionKind.PayFirstDebt => "Pay the first debt",
            DecisionPlanActionKind.UseFreedPayment => "Use the freed payment",
            DecisionPlanActionKind.ConfirmBills => "Confirm bills",
            DecisionPlanActionKind.SetFlexibleSpending => "Set flexible spending",
            DecisionPlanActionKind.ChooseCashToKeep => "Choose cash to keep",
            _ => "Review the plan"
        };
    }

    /// <summary>
    /// Labels each planning fact. Identifiers are left out. Detail stays words, not an amount.
    /// </summary>
    private static List<PlanningSharePart> FactLines(IReadOnlyList<PlanningFactDto> facts)
    {
        if (facts.Count == 0)
        {
            return Lines(PlanningShareSection.Facts, "No planning facts are recorded.");
        }

        return facts.Select(fact => new PlanningSharePart(PlanningShareSection.Facts, FactLine(fact))).ToList();
    }

    /// <summary>
    /// One labeled fact. A missing amount is omitted.
    /// </summary>
    private static string FactLine(PlanningFactDto fact)
    {
        var line = BasisWords(fact.Basis) + ". " + fact.Name.Trim();
        if (fact.Amount is decimal amount)
        {
            line += ", " + Money(amount, fact.Currency ?? "");
        }

        if (fact.AsOf is DateOnly date)
        {
            line += ", as of " + date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(fact.Detail))
        {
            line += ". " + fact.Detail.Trim();
        }

        return line + ".";
    }

    /// <summary>
    /// The basis words used in a share.
    /// </summary>
    private static string BasisWords(PlanningFactBasis basis)
    {
        return basis switch
        {
            PlanningFactBasis.ImportedFact => "Imported fact",
            PlanningFactBasis.Estimate => "Estimate",
            PlanningFactBasis.UnverifiedSuggestion => "Unverified suggestion",
            _ => "User statement"
        };
    }

    /// <summary>
    /// Copies the forecast wording, its assumptions, and its tradeoffs. The day-by-day figures stay out.
    /// </summary>
    private static List<PlanningSharePart> ForecastLines(PlanningForecastDto forecast)
    {
        var parts = Lines(PlanningShareSection.Forecast, forecast.Statement);
        foreach (var assumption in forecast.Assumptions)
        {
            parts.Add(new PlanningSharePart(
                PlanningShareSection.Forecast,
                assumption.Term + ": " + assumption.Detail));
        }

        foreach (var tradeoff in forecast.Tradeoffs)
        {
            parts.Add(new PlanningSharePart(PlanningShareSection.Forecast, tradeoff.Explanation));
        }

        return parts;
    }

    /// <summary>
    /// Formats an amount with its currency. A blank currency is still shown as the number.
    /// </summary>
    private static string Money(decimal amount, string currency)
    {
        var text = amount.ToString("0.00", CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(currency) ? text : text + " " + currency.Trim();
    }

    /// <summary>
    /// A count and the matching noun.
    /// </summary>
    private static string Count(int count, string singular, string plural)
    {
        return count + " " + (count == 1 ? singular : plural);
    }

    /// <summary>
    /// Cadence words for a paycheck or a bill.
    /// </summary>
    private static string Cadence(IncomeCadence cadence)
    {
        return cadence switch
        {
            IncomeCadence.Weekly => "weekly",
            IncomeCadence.Biweekly => "every two weeks",
            IncomeCadence.Semimonthly => "twice a month",
            IncomeCadence.Quarterly => "quarterly",
            IncomeCadence.Yearly => "yearly",
            IncomeCadence.Irregular => "irregular",
            _ => "monthly"
        };
    }

    /// <summary>
    /// Cadence words for a bill. The schedules match income.
    /// </summary>
    private static string Cadence(ObligationCadence cadence)
    {
        return cadence switch
        {
            ObligationCadence.Weekly => "weekly",
            ObligationCadence.Biweekly => "every two weeks",
            ObligationCadence.Semimonthly => "twice a month",
            ObligationCadence.Quarterly => "quarterly",
            ObligationCadence.Yearly => "yearly",
            ObligationCadence.Irregular => "irregular",
            _ => "monthly"
        };
    }

    #endregion
}
