using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Debts;
using Cardui.Api.Domain.Planning;
using Cardui.Api.Dtos.Account;
using Cardui.Api.Dtos.Debts;
using Cardui.Api.Dtos.Income;
using Cardui.Api.Dtos.Living;
using Cardui.Api.Dtos.Obligations;
using Cardui.Api.Dtos.Planning;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;

namespace Cardui.Api.Services.Implementations;

public class PlanningFacts : IPlanningFacts
{
    private readonly IAccountsService _accountsService;
    private readonly IIncomeSourcesService _incomeSourcesService;
    private readonly IObligationsService _obligationsService;
    private readonly IDebtsService _debtsService;
    private readonly ILivingService _livingService;
    private readonly IConversationService _conversationService;
    private readonly HouseholdScope _householdScope;

    public PlanningFacts(
        IAccountsService accountsService,
        IIncomeSourcesService incomeSourcesService,
        IObligationsService obligationsService,
        IDebtsService debtsService,
        ILivingService livingService,
        IConversationService conversationService,
        HouseholdScope householdScope)
    {
        _accountsService = accountsService;
        _incomeSourcesService = incomeSourcesService;
        _obligationsService = obligationsService;
        _debtsService = debtsService;
        _livingService = livingService;
        _conversationService = conversationService;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PlanningFactDto>> DescribeAsync(
        CancellationToken cancellationToken = default)
    {
        RequireHousehold();
        var subjects = await LoadSubjectsAsync(cancellationToken);
        return subjects.Select(Describe).ToList();
    }

    #region Private Methods

    /// <summary>
    /// Requires a household before any input is labeled.
    /// </summary>
    private void RequireHousehold()
    {
        _householdScope.RequireHouseholdId();
    }

    /// <summary>
    /// Reads the inputs the label rule can see, in the order they are returned.
    /// Cash, pay, saved bills, suggestions, debts, living spending, then notes.
    /// The reads are the same ones the screens use. Nothing is written.
    /// </summary>
    private async Task<List<PlanningFactSubject>> LoadSubjectsAsync(CancellationToken cancellationToken)
    {
        var subjects = new List<PlanningFactSubject>();
        var accounts = await _accountsService.GetAccountsAsync(cancellationToken: cancellationToken);
        subjects.AddRange(CashSubjects(accounts));
        var incomes = await _incomeSourcesService.GetIncomeSourcesAsync(cancellationToken);
        subjects.AddRange(IncomeSubjects(incomes));
        var bills = await _obligationsService.GetObligationsAsync(cancellationToken);
        subjects.AddRange(BillSubjects(bills));
        var suggestions = await _obligationsService.GetSuggestionsAsync(cancellationToken);
        subjects.AddRange(SuggestionSubjects(suggestions));
        await AddDebtSubjectsAsync(subjects, cancellationToken);
        var living = await _livingService.GetAsync(cancellationToken);
        subjects.AddRange(LivingSubjects(living));
        subjects.AddRange(await NoteSubjectsAsync(cancellationToken));
        return subjects;
    }

    /// <summary>
    /// Labels cash the household can hold.
    /// A credit card, a loan, and an investment are left out, so a card balance does not become cash or a debt.
    /// </summary>
    private static List<PlanningFactSubject> CashSubjects(IReadOnlyList<AccountDto> accounts)
    {
        return accounts
            .Where(account => AccountTypes.IsCash(account.Type))
            .Select(account => new PlanningFactSubject(
                PlanningFactKind.CashBalance,
                account.Id,
                null,
                account.Name,
                null,
                account.CurrentBalance,
                account.IsoCurrencyCode,
                null,
                account.Source,
                null))
            .ToList();
    }

    /// <summary>
    /// Labels each paycheck, then its monthly average when the cadence has one.
    /// The payment keeps its date. Irregular pay has no average. Low, strong, and gross stay on the income record.
    /// </summary>
    private static List<PlanningFactSubject> IncomeSubjects(IReadOnlyList<IncomeSourceDto> incomes)
    {
        var subjects = new List<PlanningFactSubject>();
        foreach (var income in incomes)
        {
            subjects.Add(new PlanningFactSubject(
                PlanningFactKind.IncomePayment,
                income.Id,
                null,
                income.Name,
                null,
                income.TakeHomeAmount,
                income.Currency,
                income.NextPaymentDate,
                null,
                null));

            if (income.AverageMonthlyAmount is not decimal average)
            {
                continue;
            }

            subjects.Add(new PlanningFactSubject(
                PlanningFactKind.IncomeMonthlyAverage,
                income.Id,
                null,
                income.Name,
                null,
                average,
                income.Currency,
                null,
                null,
                null));
        }

        return subjects;
    }

    /// <summary>
    /// Labels bills the person saved.
    /// A suggestion is not in this list.
    /// </summary>
    private static List<PlanningFactSubject> BillSubjects(IReadOnlyList<ObligationDto> bills)
    {
        return bills
            .Select(bill => new PlanningFactSubject(
                PlanningFactKind.Bill,
                bill.Id,
                bill.AccountId,
                bill.Name,
                null,
                bill.Amount,
                bill.Currency,
                bill.NextDueDate,
                null,
                null))
            .ToList();
    }

    /// <summary>
    /// Labels recurring patterns that are not bills yet.
    /// The key stays in the detail. The pattern is not saved here.
    /// </summary>
    private static List<PlanningFactSubject> SuggestionSubjects(
        IReadOnlyList<ObligationSuggestionDto> suggestions)
    {
        return suggestions
            .Select(suggestion => new PlanningFactSubject(
                PlanningFactKind.RecurringExpense,
                null,
                suggestion.AccountId,
                suggestion.Name,
                suggestion.Key,
                suggestion.Amount,
                suggestion.Currency,
                suggestion.NextDueDate,
                null,
                null))
            .ToList();
    }

    /// <summary>
    /// Labels each debt's known terms, then any account that only looks like that debt.
    /// A missing term is omitted. An account the debt already follows is not suggested again.
    /// </summary>
    private async Task AddDebtSubjectsAsync(
        List<PlanningFactSubject> subjects,
        CancellationToken cancellationToken)
    {
        var debts = await _debtsService.GetDebtsAsync(cancellationToken);
        foreach (var debt in debts)
        {
            subjects.AddRange(DebtFieldSubjects(debt));
            var accounts = await _debtsService.GetFollowAccountsAsync(debt.Id, cancellationToken);
            subjects.AddRange(DebtMatchSubjects(debt, accounts));
        }
    }

    /// <summary>
    /// The balance, limit, minimum, due date, and rate the debt already has.
    /// A null term stays missing. Zero is kept when the person entered zero.
    /// </summary>
    private static List<PlanningFactSubject> DebtFieldSubjects(DebtDto debt)
    {
        var subjects = new List<PlanningFactSubject>();
        if (debt.BalanceInUse is decimal balance)
        {
            subjects.Add(Term(
                debt,
                PlanningFactKind.DebtBalance,
                balance,
                debt.Currency,
                debt.BalanceInUseAsOf,
                debt.BalanceSource));
        }

        if (debt.CreditLimitInUse is decimal limit)
        {
            subjects.Add(Term(
                debt,
                PlanningFactKind.DebtCreditLimit,
                limit,
                debt.Currency,
                CreditLimitAsOf(debt),
                debt.CreditLimitSource));
        }

        if (debt.MinimumPayment is decimal minimum)
        {
            subjects.Add(Term(
                debt,
                PlanningFactKind.DebtMinimum,
                minimum,
                debt.Currency,
                null,
                null));
        }

        if (debt.NextDueDate is DateOnly due)
        {
            subjects.Add(Term(debt, PlanningFactKind.DebtDueDate, null, null, due, null));
        }

        if (debt.Apr is decimal apr)
        {
            subjects.Add(Term(debt, PlanningFactKind.DebtApr, apr, null, null, null));
        }

        return subjects;
    }

    /// <summary>
    /// The date that belongs with the credit limit in use.
    /// A synced limit uses the connection date. An override uses the day the person kept their own. A typed limit has no date.
    /// </summary>
    private static DateOnly? CreditLimitAsOf(DebtDto debt)
    {
        return debt.CreditLimitSource switch
        {
            DebtFieldSource.Synced => debt.SyncedCreditLimitAsOf,
            DebtFieldSource.Override => debt.CreditLimitOverriddenOn,
            _ => null
        };
    }

    /// <summary>
    /// One subject for a debt term. The field source is set only for the balance and the credit limit.
    /// </summary>
    private static PlanningFactSubject Term(
        DebtDto debt,
        PlanningFactKind kind,
        decimal? amount,
        string? currency,
        DateOnly? asOf,
        DebtFieldSource? fieldSource)
    {
        return new PlanningFactSubject(
            kind,
            debt.Id,
            null,
            debt.Name,
            null,
            amount,
            currency,
            asOf,
            null,
            fieldSource);
    }

    /// <summary>
    /// Suggested accounts for one debt, strongest first.
    /// The account this debt already follows is left out. Choosing it is a separate step, and this read does not take it.
    /// </summary>
    private static List<PlanningFactSubject> DebtMatchSubjects(
        DebtDto debt,
        IReadOnlyList<DebtFollowAccountDto> accounts)
    {
        return accounts
            .Where(account => account.SuggestionOrder is not null)
            .Where(account => !debt.Following || debt.AccountId != account.AccountId)
            .OrderBy(account => account.SuggestionOrder)
            .Select(account => new PlanningFactSubject(
                PlanningFactKind.DebtMatch,
                debt.Id,
                account.AccountId,
                debt.Name,
                account.Name,
                null,
                null,
                null,
                null,
                null))
            .ToList();
    }

    /// <summary>
    /// Labels monthly living spending when the person has set it.
    /// The amount is the one they saved. It is not a bill.
    /// </summary>
    private static List<PlanningFactSubject> LivingSubjects(LivingPageDto living)
    {
        if (living.LivingSpending is not { MonthlyAmount: decimal monthly } spending)
        {
            return [];
        }

        return
        [
            new PlanningFactSubject(
                PlanningFactKind.LivingSpending,
                spending.Id,
                null,
                spending.Name,
                null,
                monthly,
                spending.Currency,
                null,
                null,
                null)
        ];
    }

    /// <summary>
    /// Notes on the open conversation.
    /// The value stays text. No open conversation means there is nothing to label, and none is started.
    /// </summary>
    private async Task<List<PlanningFactSubject>> NoteSubjectsAsync(CancellationToken cancellationToken)
    {
        var open = await _conversationService.GetOpenAsync(cancellationToken);
        if (open is null)
        {
            return [];
        }

        return open.Preferences
            .OrderBy(preference => preference.Key, StringComparer.OrdinalIgnoreCase)
            .Select(preference => new PlanningFactSubject(
                PlanningFactKind.ConversationNote,
                preference.Id,
                null,
                preference.Key,
                preference.Value,
                null,
                null,
                null,
                null,
                null))
            .ToList();
    }

    /// <summary>
    /// Labels one checked input.
    /// A subject built here already has the source the rule requires.
    /// </summary>
    private static PlanningFactDto Describe(PlanningFactSubject subject)
    {
        if (!PlanningFactBasisRules.TryDescribe(subject, out var fact, out var error))
        {
            throw new InvalidOperationException(error);
        }

        return ToDto(fact!);
    }

    /// <summary>
    /// Copies one labeled fact into the shape a later turn can read.
    /// </summary>
    private static PlanningFactDto ToDto(PlanningFact fact)
    {
        return new PlanningFactDto
        {
            Kind = fact.Kind,
            Basis = fact.Basis,
            Unit = fact.Unit,
            ConfirmedObligation = fact.ConfirmedObligation,
            SubjectId = fact.SubjectId,
            RelatedId = fact.RelatedId,
            Name = fact.Name,
            Detail = fact.Detail,
            Amount = fact.Amount,
            Currency = fact.Currency,
            AsOf = fact.AsOf,
            Explanation = fact.Explanation
        };
    }

    #endregion
}
