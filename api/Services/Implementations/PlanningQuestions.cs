using Cardui.Api.Domain;
using Cardui.Api.Domain.Conversations;
using Cardui.Api.Domain.Planning;
using Cardui.Api.Dtos.Conversations;
using Cardui.Api.Dtos.Debts;
using Cardui.Api.Dtos.Income;
using Cardui.Api.Dtos.Planning;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;

namespace Cardui.Api.Services.Implementations;

public class PlanningQuestions : IPlanningQuestions
{
    private const string NotePrefix = "Question:";

    private readonly IAccountsService _accountsService;
    private readonly IIncomeSourcesService _incomeSourcesService;
    private readonly IObligationsService _obligationsService;
    private readonly IDebtsService _debtsService;
    private readonly ILivingService _livingService;
    private readonly IConversationService _conversationService;
    private readonly TimeProvider _timeProvider;
    private readonly HouseholdScope _householdScope;

    public PlanningQuestions(
        IAccountsService accountsService,
        IIncomeSourcesService incomeSourcesService,
        IObligationsService obligationsService,
        IDebtsService debtsService,
        ILivingService livingService,
        IConversationService conversationService,
        TimeProvider timeProvider,
        HouseholdScope householdScope)
    {
        _accountsService = accountsService;
        _incomeSourcesService = incomeSourcesService;
        _obligationsService = obligationsService;
        _debtsService = debtsService;
        _livingService = livingService;
        _conversationService = conversationService;
        _timeProvider = timeProvider;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PlanningQuestionDto>> SelectAsync(
        CancellationToken cancellationToken = default)
    {
        RequireHousehold();
        var facts = await LoadFactsAsync(cancellationToken);
        return PlanningQuestionRules.Choose(facts).Select(ToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<ConversationResumeDto> LeaveUnknownAsync(
        Guid conversationId,
        string key,
        CancellationToken cancellationToken = default)
    {
        RequireHousehold();
        var topic = RequireTopic(key, out var debtId);
        await RequireDebtWhenNamedAsync(debtId, cancellationToken);
        return await SaveUnknownAsync(conversationId, topic, debtId, cancellationToken);
    }

    #region Private Methods

    /// <summary>
    /// Requires a household before a question is chosen or a note is saved.
    /// </summary>
    private void RequireHousehold()
    {
        _householdScope.RequireHouseholdId();
    }

    /// <summary>
    /// Reads the signed-in household into the facts the question rule uses.
    /// An open conversation contributes only notes whose value is unknown.
    /// </summary>
    private async Task<PlanningQuestionFacts> LoadFactsAsync(CancellationToken cancellationToken)
    {
        var today = FinancialDate.Today(_timeProvider, _householdScope.TimeZoneId);
        var cash = await _accountsService.GetCashPositionAsync(cancellationToken);
        var incomes = await _incomeSourcesService.GetIncomeSourcesAsync(cancellationToken);
        var bills = await _obligationsService.GetObligationsAsync(cancellationToken);
        var debts = await _debtsService.GetDebtsAsync(cancellationToken);
        var living = await _livingService.GetAsync(cancellationToken);
        var settled = await ReadUnknownKeysAsync(cancellationToken);
        return new PlanningQuestionFacts(
            cash.AccountCount,
            cash.StaleConnectedAccountCount,
            cash.Total,
            _householdScope.PlanningCurrency,
            IncomeFacts(incomes),
            bills.Any(bill => bill.Flexibility == ObligationFlexibility.Essential),
            DebtFacts(debts, today),
            living.LivingSpending?.MonthlyAmount,
            settled);
    }

    /// <summary>
    /// The paychecks the question rule can see. Each amount stays on the income row.
    /// </summary>
    private static IReadOnlyList<PlanningIncomeFact> IncomeFacts(IReadOnlyList<IncomeSourceDto> incomes)
    {
        return incomes
            .Select(income => new PlanningIncomeFact(
                income.Id,
                income.Name,
                income.Reliability,
                income.NextPaymentDate))
            .ToList();
    }

    /// <summary>
    /// The debt gaps the question rule can see. A null minimum, due date, or rate stays missing.
    /// A due date before today has passed. Today has not.
    /// </summary>
    private static IReadOnlyList<PlanningDebtQuestionFact> DebtFacts(
        IReadOnlyList<DebtDto> debts,
        DateOnly today)
    {
        return debts
            .Select(debt => new PlanningDebtQuestionFact(
                debt.Id,
                debt.Name,
                debt.MinimumPayment is null,
                debt.NextDueDate is null,
                debt.Apr is null,
                debt.NextDueDate,
                debt.NextDueDate is DateOnly due && due < today))
            .ToList();
    }

    /// <summary>
    /// Question keys on the open conversation whose note value is unknown.
    /// A note under another name is ignored. No open conversation means nothing has been left unknown.
    /// </summary>
    private async Task<IReadOnlyList<string>> ReadUnknownKeysAsync(CancellationToken cancellationToken)
    {
        var open = await _conversationService.GetOpenAsync(cancellationToken);
        if (open is null)
        {
            return [];
        }

        var keys = new List<string>();
        foreach (var preference in open.Preferences)
        {
            if (!ConversationRules.SameText(preference.Value, PlanningQuestionRules.Unknown))
            {
                continue;
            }

            var key = TopicKey(preference.Key);
            if (key is not null)
            {
                keys.Add(key);
            }
        }

        return keys;
    }

    /// <summary>
    /// The question key inside a Question: note.
    /// A note the person wrote under another name returns null.
    /// </summary>
    private static string? TopicKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var text = key.Trim();
        if (!text.StartsWith(NotePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var topic = text[NotePrefix.Length..];
        return string.IsNullOrWhiteSpace(topic) ? null : topic;
    }

    /// <summary>
    /// Reads the question key. A key this rule does not define is rejected.
    /// </summary>
    private static PlanningQuestionTopic RequireTopic(string key, out Guid? debtId)
    {
        if (!PlanningQuestionRules.TryParseKey(TopicKey(key) ?? key, out var topic, out debtId))
        {
            throw new BadRequestException("Choose a question.");
        }

        return topic;
    }

    /// <summary>
    /// Requires the named debt to belong to this household.
    /// A household question has no debt id and skips this check.
    /// </summary>
    private async Task RequireDebtWhenNamedAsync(Guid? debtId, CancellationToken cancellationToken)
    {
        if (debtId is not Guid id)
        {
            return;
        }

        var debts = await _debtsService.GetDebtsAsync(cancellationToken);
        if (debts.All(debt => debt.Id != id))
        {
            throw new NotFoundException("That debt was not found.");
        }
    }

    /// <summary>
    /// Saves the unknown note through the conversation service.
    /// The canonical key is stored. Pay, bills, debts, cash, and living spending are not changed.
    /// </summary>
    private async Task<ConversationResumeDto> SaveUnknownAsync(
        Guid conversationId,
        PlanningQuestionTopic topic,
        Guid? debtId,
        CancellationToken cancellationToken)
    {
        return await _conversationService.SetPreferenceAsync(
            conversationId,
            NotePrefix + PlanningQuestionRules.Key(topic, debtId),
            PlanningQuestionRules.Unknown,
            cancellationToken);
    }

    /// <summary>
    /// Copies one chosen question into the shape a later turn can show.
    /// </summary>
    private static PlanningQuestionDto ToDto(PlanningQuestion question)
    {
        return new PlanningQuestionDto
        {
            Topic = question.Topic,
            Key = question.Key,
            DebtId = question.DebtId,
            Text = question.Text,
            Reason = question.Reason
        };
    }

    #endregion
}
