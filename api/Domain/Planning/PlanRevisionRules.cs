using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Cardui.Api.Domain.Plans;
using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Domain.Planning;

/// <summary>
/// Compares a proposed decision plan with the accepted plan and checks the versions the review named.
/// The numbers come from the plan that was calculated. This rule does not save, accept, or call a model.
/// A matching document is not a change. A moved conversation, draft, accepted plan, or calculation is stale.
/// </summary>
public static class PlanRevisionRules
{
    /// <summary>
    /// Accept was called without the review that displayed the proposal.
    /// </summary>
    public const string ReviewRequired = "Review the proposal before accepting it.";

    /// <summary>
    /// The review token could not be read. Accept does not guess which proposal was meant.
    /// </summary>
    public const string UnreadableReview =
        "This review could not be read. Review the proposal again.";

    private const string TokenVersion = "v1";

    private const int FingerprintLength = 64;

    /// <summary>
    /// Lists what accepting would change in the kept plan.
    /// A household with no accepted plan sees the choice, scenario, and actions that are not the baseline.
    /// The baseline keeps every freed dollar rolling forward, sends no extra, and does not name a constraint.
    /// Starting cash, living spending, and the cash floor are compared only with an accepted plan.
    /// When the documents differ and none of those fields do, the list is one calculated-plan change with no amount.
    /// </summary>
    public static IReadOnlyList<PlanRevisionChange> Changes(
        DecisionPlan? accepted,
        DecisionPlan proposed)
    {
        var changes = new List<PlanRevisionChange>();
        AddKind(changes, accepted?.Kind, proposed.Kind);
        AddScenario(changes, accepted?.Scenario, proposed.Scenario);
        AddActions(changes, accepted?.Actions, proposed.Actions);
        AddCapturedAmounts(changes, accepted, proposed);
        return changes;
    }

    /// <summary>
    /// The calculated plan as a stable hex fingerprint.
    /// The same document always produces the same text. A different document does not.
    /// </summary>
    public static string Fingerprint(DecisionPlan plan)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(DecisionPlanJson.Serialize(plan)));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Writes the versions into the token accept must send back.
    /// The token is a version check, not a secret.
    /// </summary>
    public static string Token(PlanRevisionContext context)
    {
        return string.Join(
            '|',
            TokenVersion,
            context.ConversationId.ToString("N"),
            context.ConversationUpdatedAtUtcTicks.ToString(CultureInfo.InvariantCulture),
            GuidText(context.OpenDraftId),
            GuidText(context.AcceptedPlanId),
            context.PlanFingerprint);
    }

    /// <summary>
    /// Reads a token this rule wrote. A blank, damaged, or unknown token returns false.
    /// </summary>
    public static bool TryRead(string? token, out PlanRevisionContext? context)
    {
        context = null;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var parts = token.Split('|');
        if (parts.Length != 6 || parts[0] != TokenVersion)
        {
            return false;
        }

        if (!Guid.TryParseExact(parts[1], "N", out var conversationId))
        {
            return false;
        }

        if (!long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks))
        {
            return false;
        }

        if (!TryGuid(parts[3], out var draftId) || !TryGuid(parts[4], out var acceptedId))
        {
            return false;
        }

        if (!IsFingerprint(parts[5]))
        {
            return false;
        }

        context = new PlanRevisionContext(
            conversationId,
            ticks,
            draftId,
            acceptedId,
            parts[5]);
        return true;
    }

    /// <summary>
    /// The first version that no longer matches, or null when the review is still current.
    /// The conversation is checked first, then the draft, then the accepted plan, then the calculation.
    /// </summary>
    public static PlanRevisionStaleReason? Stale(
        PlanRevisionContext current,
        PlanRevisionContext expected)
    {
        if (current.ConversationId != expected.ConversationId
            || current.ConversationUpdatedAtUtcTicks != expected.ConversationUpdatedAtUtcTicks)
        {
            return PlanRevisionStaleReason.Conversation;
        }

        if (current.OpenDraftId != expected.OpenDraftId)
        {
            return PlanRevisionStaleReason.Draft;
        }

        if (current.AcceptedPlanId != expected.AcceptedPlanId)
        {
            return PlanRevisionStaleReason.Accepted;
        }

        if (!string.Equals(current.PlanFingerprint, expected.PlanFingerprint, StringComparison.Ordinal))
        {
            return PlanRevisionStaleReason.Plan;
        }

        return null;
    }

    /// <summary>
    /// The words for a stale review. Accept uses the same sentence and does not save.
    /// </summary>
    public static string Message(PlanRevisionStaleReason reason)
    {
        return reason switch
        {
            PlanRevisionStaleReason.Conversation =>
                "This conversation changed. Review the proposal again.",
            PlanRevisionStaleReason.Draft =>
                "The plan draft changed outside this conversation. Review the proposal again.",
            PlanRevisionStaleReason.Accepted =>
                "The accepted plan changed outside this conversation. Review the proposal again.",
            PlanRevisionStaleReason.Plan =>
                "The plan changed outside this conversation. Review the proposal again.",
            _ => "Review the proposal again."
        };
    }

    /// <summary>
    /// The versions the current conversation and plan rows bind to a calculated plan.
    /// </summary>
    public static PlanRevisionContext Context(PlanRevisionBaseline baseline, string fingerprint)
    {
        return new PlanRevisionContext(
            baseline.ConversationId,
            baseline.UpdatedAt.UtcTicks,
            baseline.OpenDraftId,
            baseline.AcceptedPlanId,
            fingerprint);
    }

    #region Private Methods

    /// <summary>
    /// Adds the freed-cash choice when it is not the accepted one.
    /// A first plan names the choice only when it does not roll every freed dollar forward.
    /// </summary>
    private static void AddKind(
        List<PlanRevisionChange> changes,
        PayoffRolloverKind? accepted,
        PayoffRolloverKind proposed)
    {
        if (accepted is null)
        {
            if (proposed != PayoffRolloverKind.Rollover)
            {
                changes.Add(new PlanRevisionChange(
                    PlanRevisionField.FreedCash,
                    AfterKind: proposed));
            }

            return;
        }

        if (accepted != proposed)
        {
            changes.Add(new PlanRevisionChange(
                PlanRevisionField.FreedCash,
                BeforeKind: accepted,
                AfterKind: proposed));
        }
    }

    /// <summary>
    /// Adds each scenario field that differs.
    /// A first plan skips a field that is still the baseline.
    /// </summary>
    private static void AddScenario(
        List<PlanRevisionChange> changes,
        DecisionPlanScenario? accepted,
        DecisionPlanScenario proposed)
    {
        var hasAccepted = accepted is not null;
        AddAmount(
            changes,
            PlanRevisionField.MonthlyExtra,
            accepted?.MonthlyExtra,
            proposed.MonthlyExtra,
            hasAccepted);
        AddCount(
            changes,
            PlanRevisionField.IncomeLoss,
            accepted?.IncomeLossMonths,
            proposed.IncomeLossMonths,
            hasAccepted);
        AddAmount(
            changes,
            PlanRevisionField.Windfall,
            accepted?.WindfallAmount,
            proposed.WindfallAmount,
            hasAccepted);
        AddDate(
            changes,
            PlanRevisionField.WindfallDate,
            hasAccepted ? accepted!.WindfallOn : null,
            proposed.WindfallOn,
            hasAccepted);
        AddOptionalAmount(
            changes,
            PlanRevisionField.FlexibleSpending,
            hasAccepted ? accepted!.LivingSpendingMonthly : null,
            proposed.LivingSpendingMonthly,
            hasAccepted);
        AddOptionalAmount(
            changes,
            PlanRevisionField.CashToKeep,
            hasAccepted ? accepted!.CashFloor : null,
            proposed.CashFloor,
            hasAccepted);
        AddPriority(changes, accepted, proposed, hasAccepted);
        AddConstraint(changes, accepted, proposed, hasAccepted);
        AddAmount(
            changes,
            PlanRevisionField.Reclaim,
            accepted?.ReclaimAmount,
            proposed.ReclaimAmount,
            hasAccepted);
    }

    /// <summary>
    /// Adds a required amount when it differs from the accepted plan, or from zero on a first plan.
    /// </summary>
    private static void AddAmount(
        List<PlanRevisionChange> changes,
        PlanRevisionField field,
        decimal? acceptedAmount,
        decimal proposedAmount,
        bool hasAccepted)
    {
        if (hasAccepted)
        {
            if (acceptedAmount != proposedAmount)
            {
                changes.Add(new PlanRevisionChange(
                    field,
                    BeforeAmount: acceptedAmount,
                    AfterAmount: proposedAmount));
            }

            return;
        }

        if (proposedAmount != 0)
        {
            changes.Add(new PlanRevisionChange(field, AfterAmount: proposedAmount));
        }
    }

    /// <summary>
    /// Adds a count when it differs from the accepted plan, or from zero on a first plan.
    /// </summary>
    private static void AddCount(
        List<PlanRevisionChange> changes,
        PlanRevisionField field,
        int? acceptedCount,
        int proposedCount,
        bool hasAccepted)
    {
        if (hasAccepted)
        {
            if (acceptedCount != proposedCount)
            {
                changes.Add(new PlanRevisionChange(
                    field,
                    BeforeCount: acceptedCount,
                    AfterCount: proposedCount));
            }

            return;
        }

        if (proposedCount != 0)
        {
            changes.Add(new PlanRevisionChange(field, AfterCount: proposedCount));
        }
    }

    /// <summary>
    /// Adds a date when it differs. A first plan names a date only when the proposal has one.
    /// </summary>
    private static void AddDate(
        List<PlanRevisionChange> changes,
        PlanRevisionField field,
        DateOnly? acceptedOn,
        DateOnly? proposedOn,
        bool hasAccepted)
    {
        if (hasAccepted)
        {
            if (acceptedOn != proposedOn)
            {
                changes.Add(new PlanRevisionChange(
                    field,
                    BeforeOn: acceptedOn,
                    AfterOn: proposedOn));
            }

            return;
        }

        if (proposedOn is not null)
        {
            changes.Add(new PlanRevisionChange(field, AfterOn: proposedOn));
        }
    }

    /// <summary>
    /// Adds an optional amount when it differs. A first plan names it only when the proposal sets it.
    /// </summary>
    private static void AddOptionalAmount(
        List<PlanRevisionChange> changes,
        PlanRevisionField field,
        decimal? acceptedAmount,
        decimal? proposedAmount,
        bool hasAccepted)
    {
        if (hasAccepted)
        {
            if (acceptedAmount != proposedAmount)
            {
                changes.Add(new PlanRevisionChange(
                    field,
                    BeforeAmount: acceptedAmount,
                    AfterAmount: proposedAmount));
            }

            return;
        }

        if (proposedAmount is not null)
        {
            changes.Add(new PlanRevisionChange(field, AfterAmount: proposedAmount));
        }
    }

    /// <summary>
    /// Adds the payoff priority when it is not the accepted one.
    /// A first plan names it only when it is not avalanche.
    /// </summary>
    private static void AddPriority(
        List<PlanRevisionChange> changes,
        DecisionPlanScenario? accepted,
        DecisionPlanScenario proposed,
        bool hasAccepted)
    {
        if (hasAccepted)
        {
            if (accepted!.Priority != proposed.Priority)
            {
                changes.Add(new PlanRevisionChange(
                    PlanRevisionField.PayoffPriority,
                    BeforePriority: accepted.Priority,
                    AfterPriority: proposed.Priority));
            }

            return;
        }

        if (proposed.Priority != PayoffPriorityChoice.Avalanche)
        {
            changes.Add(new PlanRevisionChange(
                PlanRevisionField.PayoffPriority,
                AfterPriority: proposed.Priority));
        }
    }

    /// <summary>
    /// Adds the constrained debt when the debt or the direction differs.
    /// A first plan names it only when the proposal has one.
    /// </summary>
    private static void AddConstraint(
        List<PlanRevisionChange> changes,
        DecisionPlanScenario? accepted,
        DecisionPlanScenario proposed,
        bool hasAccepted)
    {
        var proposedHasConstraint = proposed.ConstraintDebtId is not null
            || proposed.ConstraintKind is not null;
        if (!hasAccepted)
        {
            if (proposedHasConstraint)
            {
                changes.Add(new PlanRevisionChange(
                    PlanRevisionField.Constraint,
                    AfterDebtId: proposed.ConstraintDebtId,
                    AfterConstraint: proposed.ConstraintKind));
            }

            return;
        }

        if (accepted!.ConstraintDebtId != proposed.ConstraintDebtId
            || accepted.ConstraintKind != proposed.ConstraintKind)
        {
            changes.Add(new PlanRevisionChange(
                PlanRevisionField.Constraint,
                BeforeDebtId: accepted.ConstraintDebtId,
                AfterDebtId: proposed.ConstraintDebtId,
                BeforeConstraint: accepted.ConstraintKind,
                AfterConstraint: proposed.ConstraintKind));
        }
    }

    /// <summary>
    /// Adds the captured cash, spending, and floor when an accepted plan used different amounts.
    /// A first plan leaves these out. The actions already say what that plan would start from.
    /// When the documents differ and nothing else was listed, one calculated-plan change is added.
    /// That change has no amount. The fingerprint still identifies the document.
    /// </summary>
    private static void AddCapturedAmounts(
        List<PlanRevisionChange> changes,
        DecisionPlan? accepted,
        DecisionPlan proposed)
    {
        if (accepted is null)
        {
            return;
        }

        AddCapturedAmount(
            changes,
            PlanRevisionField.StartingCash,
            accepted.Facts.StartingCash,
            proposed.Facts.StartingCash);
        AddCapturedAmount(
            changes,
            PlanRevisionField.LivingSpending,
            accepted.Facts.LivingSpendingMonthly,
            proposed.Facts.LivingSpendingMonthly);
        AddCapturedAmount(
            changes,
            PlanRevisionField.CashFloor,
            accepted.Facts.CashFloor,
            proposed.Facts.CashFloor);
        if (changes.Count == 0 && !DecisionPlanRules.Same(accepted, proposed))
        {
            changes.Add(new PlanRevisionChange(PlanRevisionField.CalculatedPlan));
        }
    }

    /// <summary>
    /// Adds one captured amount when the accepted plan used a different figure.
    /// </summary>
    private static void AddCapturedAmount(
        List<PlanRevisionChange> changes,
        PlanRevisionField field,
        decimal acceptedAmount,
        decimal proposedAmount)
    {
        if (acceptedAmount == proposedAmount)
        {
            return;
        }

        changes.Add(new PlanRevisionChange(
            field,
            BeforeAmount: acceptedAmount,
            AfterAmount: proposedAmount));
    }

    /// <summary>
    /// Adds an action whose rank, kind, or figures differ.
    /// Ranks line up from the first action. A dropped action has no after-value.
    /// </summary>
    private static void AddActions(
        List<PlanRevisionChange> changes,
        IReadOnlyList<DecisionPlanAction>? accepted,
        IReadOnlyList<DecisionPlanAction> proposed)
    {
        var current = accepted ?? [];
        var count = Math.Max(current.Count, proposed.Count);
        for (var index = 0; index < count; index++)
        {
            var before = index < current.Count ? current[index] : null;
            var after = index < proposed.Count ? proposed[index] : null;
            if (before == after)
            {
                continue;
            }

            changes.Add(ActionChange(before, after));
        }
    }

    /// <summary>
    /// Copies one action difference. A missing side leaves that side's fields empty.
    /// </summary>
    private static PlanRevisionChange ActionChange(
        DecisionPlanAction? before,
        DecisionPlanAction? after)
    {
        return new PlanRevisionChange(
            PlanRevisionField.Action,
            Order: after?.Order ?? before?.Order,
            BeforeAction: before?.Kind,
            AfterAction: after?.Kind,
            BeforeAmount: before?.Amount,
            AfterAmount: after?.Amount,
            BeforeExtra: before?.Extra,
            AfterExtra: after?.Extra,
            BeforeCount: before?.Count,
            AfterCount: after?.Count,
            BeforeOn: before?.On,
            AfterOn: after?.On,
            BeforeName: before?.Name,
            AfterName: after?.Name,
            BeforeNextName: before?.NextName,
            AfterNextName: after?.NextName,
            BeforeAlreadyShort: before is null ? null : before.AlreadyShortWithoutExtra,
            AfterAlreadyShort: after is null ? null : after.AlreadyShortWithoutExtra);
    }

    /// <summary>
    /// A guid in the token, or a dash when the household has no row in that status.
    /// </summary>
    private static string GuidText(Guid? id)
    {
        return id is Guid value ? value.ToString("N") : "-";
    }

    /// <summary>
    /// Reads a guid slot. A dash means there is no id. Any other text has to be a guid.
    /// </summary>
    private static bool TryGuid(string text, out Guid? id)
    {
        if (text == "-")
        {
            id = null;
            return true;
        }

        if (Guid.TryParseExact(text, "N", out var value))
        {
            id = value;
            return true;
        }

        id = null;
        return false;
    }

    /// <summary>
    /// True when the text is the lowercase hex fingerprint this rule writes.
    /// </summary>
    private static bool IsFingerprint(string text)
    {
        if (text.Length != FingerprintLength)
        {
            return false;
        }

        foreach (var character in text)
        {
            var hex = character is >= '0' and <= '9' or >= 'a' and <= 'f';
            if (!hex)
            {
                return false;
            }
        }

        return true;
    }

    #endregion
}
