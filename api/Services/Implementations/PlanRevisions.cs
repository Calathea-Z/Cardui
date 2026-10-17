using Cardui.Api.Data;
using Cardui.Api.Domain.Conversations;
using Cardui.Api.Domain.Planning;
using Cardui.Api.Domain.Plans;
using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Plan;
using Cardui.Api.Dtos.Planning;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class PlanRevisions : IPlanRevisions
{
    private const string ConversationNotFound = "That conversation was not found.";

    private readonly CarduiDBContext _dbContext;
    private readonly IPlanService _planService;
    private readonly ISavedPlanService _savedPlanService;
    private readonly HouseholdScope _householdScope;

    public PlanRevisions(
        CarduiDBContext dbContext,
        IPlanService planService,
        ISavedPlanService savedPlanService,
        HouseholdScope householdScope)
    {
        _dbContext = dbContext;
        _planService = planService;
        _savedPlanService = savedPlanService;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<PlanRevisionReviewDto> ReviewAsync(
        Guid conversationId,
        PlanScenarioQuery scenario,
        PayoffRolloverKind kind,
        string? reviewToken = null,
        CancellationToken cancellationToken = default)
    {
        RequireHousehold();
        var expected = ReadOptionalToken(reviewToken);
        var baseline = await LoadBaselineAsync(conversationId, cancellationToken);
        var proposed = await ComposeAsync(scenario, kind, cancellationToken);
        var current = PlanRevisionRules.Context(
            baseline,
            PlanRevisionRules.Fingerprint(proposed));
        if (expected is not null
            && PlanRevisionRules.Stale(current, expected) is PlanRevisionStaleReason reason)
        {
            return StaleReview(baseline, reason);
        }

        return CurrentReview(baseline, proposed, current);
    }

    /// <inheritdoc />
    public async Task<PlanVersionDto> AcceptAsync(
        Guid conversationId,
        PlanScenarioQuery scenario,
        PayoffRolloverKind kind,
        string reviewToken,
        CancellationToken cancellationToken = default)
    {
        RequireHousehold();
        var expected = RequireToken(reviewToken);
        var baseline = await LoadBaselineAsync(conversationId, cancellationToken);
        var proposed = await ComposeAsync(scenario, kind, cancellationToken);
        var current = PlanRevisionRules.Context(
            baseline,
            PlanRevisionRules.Fingerprint(proposed));
        if (PlanRevisionRules.Stale(current, expected) == PlanRevisionStaleReason.Conversation)
        {
            throw new ConflictException(
                PlanRevisionRules.Message(PlanRevisionStaleReason.Conversation));
        }

        var existing = await AlreadyAcceptedAsync(baseline, proposed, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        if (PlanRevisionRules.Stale(current, expected) is PlanRevisionStaleReason reason)
        {
            throw new ConflictException(PlanRevisionRules.Message(reason));
        }

        var draft = await _savedPlanService.SaveDraftAsync(
            ToSave(scenario, kind, baseline.OpenDraftId),
            cancellationToken);
        return await _savedPlanService.AcceptAsync(draft.Id, cancellationToken);
    }

    #region Private Methods

    /// <summary>
    /// The accepted plan when this proposal is already the one the household keeps.
    /// A repeated accept returns it and does not write another version.
    /// The caller has already rejected a conversation that moved.
    /// </summary>
    private async Task<PlanVersionDto?> AlreadyAcceptedAsync(
        PlanRevisionBaseline baseline,
        DecisionPlan proposed,
        CancellationToken cancellationToken)
    {
        if (baseline.AcceptedPlan is null
            || !DecisionPlanRules.Same(baseline.AcceptedPlan, proposed))
        {
            return null;
        }

        return await _savedPlanService.GetAsync(
            baseline.AcceptedPlanId!.Value,
            cancellationToken);
    }

    /// <summary>
    /// Requires a household before a proposal is read or accepted.
    /// </summary>
    private void RequireHousehold()
    {
        _householdScope.RequireHouseholdId();
    }

    /// <summary>
    /// Reads a review token sent back with a new look at the same proposal.
    /// A missing token starts a new review. A damaged token is rejected.
    /// </summary>
    private static PlanRevisionContext? ReadOptionalToken(string? reviewToken)
    {
        if (string.IsNullOrWhiteSpace(reviewToken))
        {
            return null;
        }

        return RequireToken(reviewToken);
    }

    /// <summary>
    /// Reads the token accept must send. A missing or damaged token is rejected.
    /// </summary>
    private static PlanRevisionContext RequireToken(string? reviewToken)
    {
        if (string.IsNullOrWhiteSpace(reviewToken))
        {
            throw new BadRequestException(PlanRevisionRules.ReviewRequired);
        }

        if (!PlanRevisionRules.TryRead(reviewToken, out var context) || context is null)
        {
            throw new BadRequestException(PlanRevisionRules.UnreadableReview);
        }

        return context;
    }

    /// <summary>
    /// Loads the open conversation and the household's draft and accepted plans.
    /// A closed conversation is rejected. Another household's conversation is not found.
    /// </summary>
    private async Task<PlanRevisionBaseline> LoadBaselineAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var conversation = await _dbContext.Conversations
            .AsNoTracking()
            .Where(item => item.HouseholdId == householdId && item.Id == conversationId)
            .Select(item => new
            {
                item.Id,
                item.Status,
                item.UpdatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (conversation is null)
        {
            throw new NotFoundException(ConversationNotFound);
        }

        var closed = ConversationRules.RejectClosed(conversation.Status);
        if (closed is not null)
        {
            throw new BadRequestException(closed);
        }

        var versions = await _dbContext.PlanVersions
            .AsNoTracking()
            .Where(version => version.HouseholdId == householdId)
            .Where(version =>
                version.Status == PlanVersionStatus.Draft
                || version.Status == PlanVersionStatus.Accepted)
            .Select(version => new
            {
                version.Id,
                version.Status,
                version.Document
            })
            .ToListAsync(cancellationToken);
        var draft = versions.SingleOrDefault(version => version.Status == PlanVersionStatus.Draft);
        var accepted = versions.SingleOrDefault(version =>
            version.Status == PlanVersionStatus.Accepted);
        return new PlanRevisionBaseline(
            conversation.Id,
            conversation.Status,
            conversation.UpdatedAt,
            draft?.Id,
            draft is null ? null : DecisionPlanJson.Deserialize(draft.Document),
            accepted?.Id,
            accepted is null ? null : DecisionPlanJson.Deserialize(accepted.Document));
    }

    /// <summary>
    /// Calculates the decision plan for this scenario.
    /// The figures come from the plan service. A choice this plan cannot store is rejected.
    /// </summary>
    private async Task<DecisionPlan> ComposeAsync(
        PlanScenarioQuery scenario,
        PayoffRolloverKind kind,
        CancellationToken cancellationToken)
    {
        var message = PayoffChoiceRules.RejectKind(kind, scenario.ReclaimAmount);
        if (message is not null)
        {
            throw new BadRequestException(message);
        }

        var report = await _planService.GetRecoveryAsync(scenario, cancellationToken);
        return SavedPlanComposer.Compose(report, kind);
    }

    /// <summary>
    /// A review that still matches the conversation and the plan versions.
    /// </summary>
    private static PlanRevisionReviewDto CurrentReview(
        PlanRevisionBaseline baseline,
        DecisionPlan proposed,
        PlanRevisionContext current)
    {
        var replacesDraft = baseline.OpenDraft is not null
            && !DecisionPlanRules.Same(baseline.OpenDraft, proposed);
        return new PlanRevisionReviewDto
        {
            Applied = false,
            Stale = false,
            ConversationId = baseline.ConversationId,
            OpenDraftId = baseline.OpenDraftId,
            AcceptedPlanId = baseline.AcceptedPlanId,
            ReplacesOpenDraft = replacesDraft,
            Changes = PlanRevisionRules.Changes(baseline.AcceptedPlan, proposed)
                .Select(ToDto)
                .ToList(),
            ReviewToken = PlanRevisionRules.Token(current)
        };
    }

    /// <summary>
    /// A review whose token no longer matches. It has no proposal and cannot be accepted.
    /// </summary>
    private static PlanRevisionReviewDto StaleReview(
        PlanRevisionBaseline baseline,
        PlanRevisionStaleReason reason)
    {
        return new PlanRevisionReviewDto
        {
            Applied = false,
            Stale = true,
            Reason = reason,
            Message = PlanRevisionRules.Message(reason),
            ConversationId = baseline.ConversationId,
            OpenDraftId = baseline.OpenDraftId,
            AcceptedPlanId = baseline.AcceptedPlanId
        };
    }

    /// <summary>
    /// The draft save for a proposal that still matches the review.
    /// ReplacesDraftId is the open draft, and it is null when the household has none.
    /// </summary>
    private static SavePlanVersionDto ToSave(
        PlanScenarioQuery scenario,
        PayoffRolloverKind kind,
        Guid? replacesDraftId)
    {
        return new SavePlanVersionDto
        {
            Kind = kind,
            MonthlyExtra = scenario.MonthlyExtra,
            IncomeLossMonths = scenario.IncomeLossMonths,
            WindfallAmount = scenario.WindfallAmount,
            WindfallOn = scenario.WindfallOn,
            LivingSpendingMonthly = scenario.LivingSpendingMonthly,
            CashFloor = scenario.CashFloor,
            Priority = scenario.Priority,
            ConstraintDebtId = scenario.ConstraintDebtId,
            ConstraintKind = scenario.ConstraintKind,
            ReclaimAmount = scenario.ReclaimAmount,
            ReplacesDraftId = replacesDraftId
        };
    }

    /// <summary>
    /// The API shape of one reviewed difference.
    /// </summary>
    private static PlanRevisionChangeDto ToDto(PlanRevisionChange change)
    {
        return new PlanRevisionChangeDto
        {
            Field = change.Field,
            Order = change.Order,
            BeforeAction = change.BeforeAction,
            AfterAction = change.AfterAction,
            BeforeKind = change.BeforeKind,
            AfterKind = change.AfterKind,
            BeforePriority = change.BeforePriority,
            AfterPriority = change.AfterPriority,
            BeforeConstraint = change.BeforeConstraint,
            AfterConstraint = change.AfterConstraint,
            BeforeDebtId = change.BeforeDebtId,
            AfterDebtId = change.AfterDebtId,
            BeforeAmount = change.BeforeAmount,
            AfterAmount = change.AfterAmount,
            BeforeExtra = change.BeforeExtra,
            AfterExtra = change.AfterExtra,
            BeforeCount = change.BeforeCount,
            AfterCount = change.AfterCount,
            BeforeOn = change.BeforeOn,
            AfterOn = change.AfterOn,
            BeforeName = change.BeforeName,
            AfterName = change.AfterName,
            BeforeNextName = change.BeforeNextName,
            AfterNextName = change.AfterNextName,
            BeforeAlreadyShort = change.BeforeAlreadyShort,
            AfterAlreadyShort = change.AfterAlreadyShort
        };
    }

    #endregion
}
