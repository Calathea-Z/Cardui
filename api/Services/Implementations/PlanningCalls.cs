using Cardui.Api.Data;
using Cardui.Api.Domain.Conversations;
using Cardui.Api.Domain.Planning;
using Cardui.Api.Dtos.Planning;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class PlanningCalls : IPlanningCalls
{
    private readonly IConversationService _conversations;
    private readonly IPlanningModel _model;
    private readonly CarduiDBContext _dbContext;
    private readonly HouseholdScope _householdScope;
    private readonly ILogger<PlanningCalls> _logger;

    public PlanningCalls(
        IConversationService conversations,
        IPlanningModel model,
        CarduiDBContext dbContext,
        HouseholdScope householdScope,
        ILogger<PlanningCalls> logger)
    {
        _conversations = conversations;
        _model = model;
        _dbContext = dbContext;
        _householdScope = householdScope;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PlanningCallDto> AskAsync(
        Guid conversationId,
        PlanningModelRequest request,
        CancellationToken cancellationToken = default)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var conversation = await _conversations.GetAsync(conversationId, cancellationToken);
        Reject(ConversationRules.RejectClosed(conversation.Status));
        var draftId = await FindDraftIdAsync(householdId, cancellationToken);
        Reject(PlanningCallRules.RejectToken(request.Input, request.Instructions));
        if (PlanningCallRules.RejectLength(request.Input, request.Instructions) is not null)
        {
            return Continue(draftId, PlanningCallFallback.CharacterCap, PlanningCallRules.LimitNotice);
        }

        try
        {
            var reply = await _model.ReplyAsync(request, cancellationToken);
            return new PlanningCallDto
            {
                Result = PlanningCallResult.Answered,
                Text = reply.Text,
                DraftId = draftId
            };
        }
        catch (PlanningModelNotConfiguredException)
        {
            return Continue(
                draftId,
                PlanningCallFallback.NotConfigured,
                PlanningCallRules.NotConfiguredNotice);
        }
        catch (PlanningModelException)
        {
            return Continue(
                draftId,
                PlanningCallFallback.NoAnswer,
                PlanningCallRules.UnavailableNotice);
        }
    }

    #region Private Methods

    /// <summary>
    /// Throws when a call breaks a conversation or token rule.
    /// </summary>
    private static void Reject(string? error)
    {
        if (error is not null)
        {
            throw new BadRequestException(error);
        }
    }

    /// <summary>
    /// Reads the open draft id for this household. The plan document is not loaded.
    /// </summary>
    private async Task<Guid?> FindDraftIdAsync(
        Guid householdId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.PlanVersions
            .AsNoTracking()
            .Where(version =>
                version.HouseholdId == householdId
                && version.Status == PlanVersionStatus.Draft)
            .Select(version => (Guid?)version.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Returns the manual path and logs the reason. The person's text is not logged.
    /// </summary>
    private PlanningCallDto Continue(Guid? draftId, PlanningCallFallback reason, string notice)
    {
        _logger.LogWarning("{Detail}", PlanningCallRules.DescribeFallback(reason));
        return new PlanningCallDto
        {
            Result = PlanningCallResult.ContinueManually,
            Notice = notice,
            DraftId = draftId
        };
    }

    #endregion
}
