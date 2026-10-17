using Cardui.Api.Data;
using Cardui.Api.Domain.Conversations;
using Cardui.Api.Domain.Planning;
using Cardui.Api.Dtos.Conversations;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class ConversationService : IConversationService
{
    private const string ConversationNotFound = "That conversation was not found.";

    private const string QuestionNotFound = "That question was not found.";

    private const string PreferenceNotFound = "That preference was not found.";

    private const string PlanNotFound = "That saved plan was not found.";

    private const string PlanNotLinked = "That saved plan is not linked to this conversation.";

    private const string ConversationChanged =
        "This conversation changed. Review it and try again.";

    private readonly CarduiDBContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly HouseholdScope _householdScope;

    public ConversationService(
        CarduiDBContext dbContext,
        TimeProvider timeProvider,
        HouseholdScope householdScope)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<ConversationResumeDto?> GetOpenAsync(
        CancellationToken cancellationToken = default)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var openId = await FindOpenIdAsync(householdId, cancellationToken);
        if (openId is not Guid conversationId)
        {
            return null;
        }

        return await ResumeAsync(householdId, conversationId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConversationResumeDto> GetAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var householdId = _householdScope.RequireHouseholdId();
        return await ResumeAsync(householdId, conversationId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConversationResumeDto> StartAsync(
        CancellationToken cancellationToken = default)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var existing = await FindOpenIdAsync(householdId, cancellationToken);
        if (existing is Guid openId)
        {
            return await ResumeAsync(householdId, openId, cancellationToken);
        }

        var now = _timeProvider.GetUtcNow();
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            Status = ConversationStatus.Open,
            CreatedAt = now,
            UpdatedAt = now
        };
        _dbContext.Conversations.Add(conversation);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsOpenConversationConstraint(exception))
        {
            _dbContext.Entry(conversation).State = EntityState.Detached;
            var raced = await FindOpenIdAsync(householdId, cancellationToken);
            if (raced is not Guid racedId)
            {
                throw;
            }

            return await ResumeAsync(householdId, racedId, cancellationToken);
        }

        return await ResumeAsync(householdId, conversation.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConversationResumeDto> CloseAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await FindTrackedAsync(conversationId, cancellationToken);
        if (conversation.Status == ConversationStatus.Closed)
        {
            return await ResumeAsync(conversation.HouseholdId, conversation.Id, cancellationToken);
        }

        var now = _timeProvider.GetUtcNow();
        conversation.Status = ConversationStatus.Closed;
        conversation.ClosedAt = now;
        conversation.UpdatedAt = now;
        await SaveAsync(cancellationToken);
        return await ResumeAsync(conversation.HouseholdId, conversation.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConversationResumeDto> AppendMessageAsync(
        Guid conversationId,
        ConversationMessageRole role,
        string text,
        CancellationToken cancellationToken = default)
    {
        Reject(ConversationRules.RejectRole(role));
        Reject(ConversationRules.RejectText(text, ConversationMessage.TextMaxLength, "message"));
        var conversation = await FindTrackedAsync(conversationId, cancellationToken);
        Reject(ConversationRules.RejectClosed(conversation.Status));

        var now = _timeProvider.GetUtcNow();
        _dbContext.ConversationMessages.Add(new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Sequence = await NextSequenceAsync(conversation.Id, cancellationToken),
            Role = role,
            Text = ConversationRules.Normalize(text),
            CreatedAt = now
        });
        conversation.UpdatedAt = now;
        await SaveAsync(cancellationToken);
        return await ResumeAsync(conversation.HouseholdId, conversation.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConversationResumeDto> ReplaceSummaryAsync(
        Guid conversationId,
        string? summary,
        CancellationToken cancellationToken = default)
    {
        string? normalized = null;
        if (summary is not null)
        {
            Reject(ConversationRules.RejectText(summary, Conversation.SummaryMaxLength, "summary"));
            normalized = ConversationRules.Normalize(summary);
        }

        var conversation = await FindTrackedAsync(conversationId, cancellationToken);
        Reject(ConversationRules.RejectClosed(conversation.Status));
        if (conversation.Summary == normalized)
        {
            return await ResumeAsync(conversation.HouseholdId, conversation.Id, cancellationToken);
        }

        conversation.Summary = normalized;
        conversation.UpdatedAt = _timeProvider.GetUtcNow();
        await SaveAsync(cancellationToken);
        return await ResumeAsync(conversation.HouseholdId, conversation.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConversationResumeDto> AddQuestionAsync(
        Guid conversationId,
        string text,
        CancellationToken cancellationToken = default)
    {
        Reject(ConversationRules.RejectText(text, ConversationQuestion.TextMaxLength, "question"));
        var conversation = await FindTrackedAsync(conversationId, cancellationToken);
        Reject(ConversationRules.RejectClosed(conversation.Status));
        var normalized = ConversationRules.Normalize(text);
        if (await HasUnansweredAsync(conversation.Id, normalized, cancellationToken))
        {
            return await ResumeAsync(conversation.HouseholdId, conversation.Id, cancellationToken);
        }

        var now = _timeProvider.GetUtcNow();
        _dbContext.ConversationQuestions.Add(new ConversationQuestion
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Text = normalized,
            CreatedAt = now
        });
        conversation.UpdatedAt = now;
        await SaveAsync(cancellationToken);
        return await ResumeAsync(conversation.HouseholdId, conversation.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConversationResumeDto> AnswerQuestionAsync(
        Guid conversationId,
        Guid questionId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await FindTrackedAsync(conversationId, cancellationToken);
        Reject(ConversationRules.RejectClosed(conversation.Status));
        var question = await FindQuestionAsync(conversation.Id, questionId, cancellationToken);
        if (question is null)
        {
            throw new NotFoundException(QuestionNotFound);
        }

        if (question.AnsweredAt is not null)
        {
            return await ResumeAsync(conversation.HouseholdId, conversation.Id, cancellationToken);
        }

        var now = _timeProvider.GetUtcNow();
        question.AnsweredAt = now;
        conversation.UpdatedAt = now;
        await SaveAsync(cancellationToken);
        return await ResumeAsync(conversation.HouseholdId, conversation.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConversationResumeDto> SetPreferenceAsync(
        Guid conversationId,
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        Reject(PlanningShareRules.RejectReservedNote(key));
        Reject(ConversationRules.RejectText(key, ConversationPreference.KeyMaxLength, "preference name"));
        Reject(ConversationRules.RejectText(value, ConversationPreference.ValueMaxLength, "preference value"));
        var conversation = await FindTrackedAsync(conversationId, cancellationToken);
        Reject(ConversationRules.RejectClosed(conversation.Status));
        return await SavePreferenceAsync(
            conversation,
            ConversationRules.Normalize(key),
            ConversationRules.Normalize(value),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConversationResumeDto> SetDataSharingAsync(
        Guid conversationId,
        bool allowed,
        CancellationToken cancellationToken = default)
    {
        var conversation = await FindTrackedAsync(conversationId, cancellationToken);
        Reject(ConversationRules.RejectClosed(conversation.Status));
        return await SavePreferenceAsync(
            conversation,
            PlanningShareRules.ConsentKey,
            allowed ? PlanningShareRules.AllowedValue : PlanningShareRules.DeclinedValue,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConversationResumeDto> RemovePreferenceAsync(
        Guid conversationId,
        string key,
        CancellationToken cancellationToken = default)
    {
        Reject(PlanningShareRules.RejectReservedNote(key));
        Reject(ConversationRules.RejectText(key, ConversationPreference.KeyMaxLength, "preference name"));
        var conversation = await FindTrackedAsync(conversationId, cancellationToken);
        Reject(ConversationRules.RejectClosed(conversation.Status));
        var existing = await FindPreferenceAsync(
            conversation.Id,
            ConversationRules.Normalize(key),
            cancellationToken);
        if (existing is null)
        {
            throw new NotFoundException(PreferenceNotFound);
        }

        _dbContext.ConversationPreferences.Remove(existing);
        conversation.UpdatedAt = _timeProvider.GetUtcNow();
        await SaveAsync(cancellationToken);
        return await ResumeAsync(conversation.HouseholdId, conversation.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConversationResumeDto> LinkPlanVersionAsync(
        Guid conversationId,
        Guid planVersionId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await FindTrackedAsync(conversationId, cancellationToken);
        Reject(ConversationRules.RejectClosed(conversation.Status));
        if (await HasLinkAsync(conversation.Id, planVersionId, cancellationToken))
        {
            return await ResumeAsync(conversation.HouseholdId, conversation.Id, cancellationToken);
        }

        if (!await HasPlanAsync(conversation.HouseholdId, planVersionId, cancellationToken))
        {
            throw new NotFoundException(PlanNotFound);
        }

        var now = _timeProvider.GetUtcNow();
        _dbContext.ConversationPlanLinks.Add(new ConversationPlanLink
        {
            ConversationId = conversation.Id,
            PlanVersionId = planVersionId,
            LinkedAt = now
        });
        conversation.UpdatedAt = now;
        await SaveAsync(cancellationToken);
        return await ResumeAsync(conversation.HouseholdId, conversation.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConversationResumeDto> UnlinkPlanVersionAsync(
        Guid conversationId,
        Guid planVersionId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await FindTrackedAsync(conversationId, cancellationToken);
        Reject(ConversationRules.RejectClosed(conversation.Status));
        var link = await FindLinkAsync(conversation.Id, planVersionId, cancellationToken);
        if (link is null)
        {
            throw new NotFoundException(PlanNotLinked);
        }

        _dbContext.ConversationPlanLinks.Remove(link);
        conversation.UpdatedAt = _timeProvider.GetUtcNow();
        await SaveAsync(cancellationToken);
        return await ResumeAsync(conversation.HouseholdId, conversation.Id, cancellationToken);
    }

    #region Private Methods

    /// <summary>
    /// Turns a rule failure into a bad request. A passing rule returns null.
    /// </summary>
    private static void Reject(string? error)
    {
        if (error is not null)
        {
            throw new BadRequestException(error);
        }
    }

    /// <summary>
    /// Loads one conversation in this household so a write can change it.
    /// </summary>
    private async Task<Conversation> FindTrackedAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var conversation = await _dbContext.Conversations
            .Where(item => item.HouseholdId == householdId && item.Id == conversationId)
            .SingleOrDefaultAsync(cancellationToken);

        if (conversation is null)
        {
            throw new NotFoundException(ConversationNotFound);
        }

        return conversation;
    }

    /// <summary>
    /// The open conversation for the household, or null when none is open.
    /// </summary>
    private async Task<Guid?> FindOpenIdAsync(
        Guid householdId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Conversations
            .AsNoTracking()
            .Where(conversation =>
                conversation.HouseholdId == householdId
                && conversation.Status == ConversationStatus.Open)
            .Select(conversation => (Guid?)conversation.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// The next turn number. The first turn is 1.
    /// </summary>
    private async Task<int> NextSequenceAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var max = await _dbContext.ConversationMessages
            .Where(message => message.ConversationId == conversationId)
            .Select(message => (int?)message.Sequence)
            .MaxAsync(cancellationToken);
        return (max ?? 0) + 1;
    }

    /// <summary>
    /// True when this question is already unanswered on the conversation.
    /// The match ignores case.
    /// </summary>
    private async Task<bool> HasUnansweredAsync(
        Guid conversationId,
        string text,
        CancellationToken cancellationToken)
    {
        var open = await _dbContext.ConversationQuestions
            .AsNoTracking()
            .Where(question =>
                question.ConversationId == conversationId && question.AnsweredAt == null)
            .Select(question => question.Text)
            .ToListAsync(cancellationToken);

        return open.Any(question => ConversationRules.SameText(question, text));
    }

    /// <summary>
    /// Loads one question on this conversation, or null when it is not there.
    /// </summary>
    private async Task<ConversationQuestion?> FindQuestionAsync(
        Guid conversationId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ConversationQuestions
            .Where(question => question.ConversationId == conversationId && question.Id == questionId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Saves one conversation note. The same name, ignoring case, replaces the value and keeps the stored name.
    /// An unchanged value does not write again.
    /// </summary>
    private async Task<ConversationResumeDto> SavePreferenceAsync(
        Conversation conversation,
        string key,
        string value,
        CancellationToken cancellationToken)
    {
        var existing = await FindPreferenceAsync(conversation.Id, key, cancellationToken);
        var now = _timeProvider.GetUtcNow();
        if (existing is null)
        {
            _dbContext.ConversationPreferences.Add(new ConversationPreference
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                Key = key,
                Value = value,
                UpdatedAt = now
            });
        }
        else if (existing.Value == value)
        {
            return await ResumeAsync(conversation.HouseholdId, conversation.Id, cancellationToken);
        }
        else
        {
            existing.Value = value;
            existing.UpdatedAt = now;
        }

        conversation.UpdatedAt = now;
        await SaveAsync(cancellationToken);
        return await ResumeAsync(conversation.HouseholdId, conversation.Id, cancellationToken);
    }

    /// <summary>
    /// Loads the note with this name, ignoring case, so a write can replace or remove it.
    /// </summary>
    private async Task<ConversationPreference?> FindPreferenceAsync(
        Guid conversationId,
        string key,
        CancellationToken cancellationToken)
    {
        var preferences = await _dbContext.ConversationPreferences
            .Where(preference => preference.ConversationId == conversationId)
            .ToListAsync(cancellationToken);

        return preferences.FirstOrDefault(preference =>
            ConversationRules.SameText(preference.Key, key));
    }

    /// <summary>
    /// True when this conversation already links that plan version.
    /// </summary>
    private async Task<bool> HasLinkAsync(
        Guid conversationId,
        Guid planVersionId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ConversationPlanLinks
            .AsNoTracking()
            .AnyAsync(
                link => link.ConversationId == conversationId && link.PlanVersionId == planVersionId,
                cancellationToken);
    }

    /// <summary>
    /// True when the plan version belongs to this household.
    /// The plan document is not loaded.
    /// </summary>
    private async Task<bool> HasPlanAsync(
        Guid householdId,
        Guid planVersionId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.PlanVersions
            .AsNoTracking()
            .AnyAsync(
                version => version.HouseholdId == householdId && version.Id == planVersionId,
                cancellationToken);
    }

    /// <summary>
    /// Loads one link so it can be removed.
    /// </summary>
    private async Task<ConversationPlanLink?> FindLinkAsync(
        Guid conversationId,
        Guid planVersionId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ConversationPlanLinks
            .Where(link => link.ConversationId == conversationId && link.PlanVersionId == planVersionId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Saves the conversation change.
    /// A repeated turn number, note name, or plan link is a conflict.
    /// </summary>
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsConversationConstraint(exception))
        {
            throw new ConflictException(ConversationChanged);
        }
    }

    /// <summary>
    /// True when the database rejected a repeated turn number, note name, or plan link.
    /// </summary>
    private static bool IsConversationConstraint(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message;
        if (message is null)
        {
            return false;
        }

        return message.Contains(
                "IX_ConversationMessages_ConversationId_Sequence",
                StringComparison.Ordinal)
            || message.Contains(
                "IX_ConversationPreferences_ConversationId_Key",
                StringComparison.Ordinal)
            || message.Contains("PK_ConversationPlanLinks", StringComparison.Ordinal);
    }

    /// <summary>
    /// True when the database rejected a second open conversation for the household.
    /// </summary>
    private static bool IsOpenConversationConstraint(DbUpdateException exception)
    {
        return exception.InnerException?.Message.Contains(
            Conversation.OpenHouseholdIndexName,
            StringComparison.Ordinal) == true;
    }

    /// <summary>
    /// The conversation as it can be resumed. Missing for this household is an error.
    /// </summary>
    private async Task<ConversationResumeDto> ResumeAsync(
        Guid householdId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var resume = await ProjectAsync(householdId, conversationId, cancellationToken);
        if (resume is null)
        {
            throw new NotFoundException(ConversationNotFound);
        }

        return resume;
    }

    /// <summary>
    /// Reads the turns, open questions, notes, and plan links without tracking them.
    /// Answered questions are omitted. The plan document is not loaded.
    /// </summary>
    private async Task<ConversationResumeDto?> ProjectAsync(
        Guid householdId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var resume = await _dbContext.Conversations
            .AsNoTracking()
            .Where(conversation =>
                conversation.HouseholdId == householdId && conversation.Id == conversationId)
            .Select(conversation => new ConversationResumeDto
            {
                Id = conversation.Id,
                Status = conversation.Status,
                CreatedAt = conversation.CreatedAt,
                UpdatedAt = conversation.UpdatedAt,
                ClosedAt = conversation.ClosedAt,
                Summary = conversation.Summary
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (resume is null)
        {
            return null;
        }

        resume.Messages = await LoadMessagesAsync(householdId, conversationId, cancellationToken);
        resume.Questions = await LoadQuestionsAsync(householdId, conversationId, cancellationToken);
        resume.Preferences = await LoadPreferencesAsync(householdId, conversationId, cancellationToken);
        resume.PlanVersions = await LoadPlanLinksAsync(householdId, conversationId, cancellationToken);
        return resume;
    }

    /// <summary>
    /// The turns in stored order.
    /// </summary>
    private async Task<List<ConversationMessageDto>> LoadMessagesAsync(
        Guid householdId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ConversationMessages
            .AsNoTracking()
            .Where(message =>
                message.ConversationId == conversationId
                && message.Conversation!.HouseholdId == householdId)
            .OrderBy(message => message.Sequence)
            .Select(message => new ConversationMessageDto
            {
                Id = message.Id,
                Sequence = message.Sequence,
                Role = message.Role,
                Text = message.Text,
                CreatedAt = message.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Unanswered questions, oldest first.
    /// </summary>
    private async Task<List<ConversationQuestionDto>> LoadQuestionsAsync(
        Guid householdId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ConversationQuestions
            .AsNoTracking()
            .Where(question =>
                question.ConversationId == conversationId
                && question.Conversation!.HouseholdId == householdId
                && question.AnsweredAt == null)
            .OrderBy(question => question.CreatedAt)
            .ThenBy(question => question.Id)
            .Select(question => new ConversationQuestionDto
            {
                Id = question.Id,
                Text = question.Text,
                CreatedAt = question.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Notes on the conversation, by name.
    /// </summary>
    private async Task<List<ConversationPreferenceDto>> LoadPreferencesAsync(
        Guid householdId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ConversationPreferences
            .AsNoTracking()
            .Where(preference =>
                preference.ConversationId == conversationId
                && preference.Conversation!.HouseholdId == householdId)
            .OrderBy(preference => preference.Key)
            .Select(preference => new ConversationPreferenceDto
            {
                Id = preference.Id,
                Key = preference.Key,
                Value = preference.Value,
                UpdatedAt = preference.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Plan links, oldest first. The version's number and status are read without its document.
    /// </summary>
    private async Task<List<ConversationPlanLinkDto>> LoadPlanLinksAsync(
        Guid householdId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ConversationPlanLinks
            .AsNoTracking()
            .Where(link =>
                link.ConversationId == conversationId
                && link.Conversation!.HouseholdId == householdId)
            .OrderBy(link => link.LinkedAt)
            .ThenBy(link => link.PlanVersionId)
            .Select(link => new ConversationPlanLinkDto
            {
                PlanVersionId = link.PlanVersionId,
                Number = link.PlanVersion!.Number,
                Status = link.PlanVersion.Status,
                LinkedAt = link.LinkedAt
            })
            .ToListAsync(cancellationToken);
    }

    #endregion
}
