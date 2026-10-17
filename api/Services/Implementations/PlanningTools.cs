using Cardui.Api.Domain.Allocation;
using Cardui.Api.Domain.Debts;
using Cardui.Api.Domain.Income;
using Cardui.Api.Domain.Living;
using Cardui.Api.Domain.Obligations;
using Cardui.Api.Domain.Planning;
using Cardui.Api.Domain.Recovery;
using Cardui.Api.Domain.Savings;
using Cardui.Api.Dtos.Allocation;
using Cardui.Api.Dtos.Debts;
using Cardui.Api.Dtos.Income;
using Cardui.Api.Dtos.Living;
using Cardui.Api.Dtos.Obligations;
using Cardui.Api.Dtos.Plan;
using Cardui.Api.Dtos.Planning;
using Cardui.Api.Dtos.Savings;
using Cardui.Api.Exceptions;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;

namespace Cardui.Api.Services.Implementations;

public class PlanningTools : IPlanningTools
{
    private readonly IAccountsService _accountsService;
    private readonly IDebtsService _debtsService;
    private readonly IIncomeSourcesService _incomeSourcesService;
    private readonly IObligationsService _obligationsService;
    private readonly ISavingsGoalsService _savingsGoalsService;
    private readonly ILivingService _livingService;
    private readonly IAllocationService _allocationService;
    private readonly IFinancialProfileService _financialProfileService;
    private readonly IPlanService _planService;
    private readonly ISavedPlanService _savedPlanService;
    private readonly IRestructuringService _restructuringService;
    private readonly HouseholdScope _householdScope;

    public PlanningTools(
        IAccountsService accountsService,
        IDebtsService debtsService,
        IIncomeSourcesService incomeSourcesService,
        IObligationsService obligationsService,
        ISavingsGoalsService savingsGoalsService,
        ILivingService livingService,
        IAllocationService allocationService,
        IFinancialProfileService financialProfileService,
        IPlanService planService,
        ISavedPlanService savedPlanService,
        IRestructuringService restructuringService,
        HouseholdScope householdScope)
    {
        _accountsService = accountsService;
        _debtsService = debtsService;
        _incomeSourcesService = incomeSourcesService;
        _obligationsService = obligationsService;
        _savingsGoalsService = savingsGoalsService;
        _livingService = livingService;
        _allocationService = allocationService;
        _financialProfileService = financialProfileService;
        _planService = planService;
        _savedPlanService = savedPlanService;
        _restructuringService = restructuringService;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<PlanningSummaryDto> ReadSummaryAsync(
        PlanningSummaryKind kind,
        CancellationToken cancellationToken = default)
    {
        RequireHousehold();
        Reject(PlanningToolRules.RejectSummary(kind));
        return await LoadSummaryAsync(kind, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IncomeFactProposalDto> ProposeIncomeAsync(
        UpsertIncomeSourceDto dto,
        CancellationToken cancellationToken = default)
    {
        RequireHousehold();
        var draft = RequireIncome(dto);
        await RequireHouseholdContributorAsync(draft.ContributorId, cancellationToken);
        return new IncomeFactProposalDto { Applied = false, Income = draft };
    }

    /// <inheritdoc />
    public async Task<BillFactProposalDto> ProposeBillAsync(
        UpsertObligationDto dto,
        CancellationToken cancellationToken = default)
    {
        RequireHousehold();
        Reject(PlanningToolRules.RejectSuggestion(dto.SuggestionKey));
        var draft = RequireBill(dto);
        await RequireHouseholdAccountAsync(draft.AccountId, cancellationToken);
        return new BillFactProposalDto { Applied = false, Bill = draft };
    }

    /// <inheritdoc />
    public async Task<DebtFactProposalDto> ProposeDebtAsync(
        UpsertDebtDto dto,
        CancellationToken cancellationToken = default)
    {
        RequireHousehold();
        var draft = RequireDebt(dto);
        await RequireHouseholdAccountAsync(draft.AccountId, cancellationToken);
        return new DebtFactProposalDto { Applied = false, Debt = draft };
    }

    /// <inheritdoc />
    public async Task<SavingsFactProposalDto> ProposeSavingsAsync(
        UpsertSavingsGoalDto dto,
        CancellationToken cancellationToken = default)
    {
        RequireHousehold();
        var draft = RequireSavings(dto);
        await RequireSavingsAccountAsync(draft.AccountId, cancellationToken);
        return new SavingsFactProposalDto { Applied = false, Savings = draft };
    }

    /// <inheritdoc />
    public async Task<LivingContributionProposalDto> ProposeLivingContributionAsync(
        Guid contributorId,
        UpsertLivingContributionDto dto,
        CancellationToken cancellationToken = default)
    {
        RequireHousehold();
        var amount = RequireLivingContribution(dto);
        await RequireContributorExistsAsync(contributorId, cancellationToken);
        return new LivingContributionProposalDto
        {
            Applied = false,
            ContributorId = contributorId,
            MonthlyAmount = amount
        };
    }

    /// <inheritdoc />
    public Task<AllocationFactProposalDto> ProposeAllocationAsync(
        UpsertAllocationPreferenceDto dto,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RequireHousehold();
        var draft = RequireAllocation(dto);
        return Task.FromResult(new AllocationFactProposalDto { Applied = false, Allocation = draft });
    }

    /// <inheritdoc />
    public async Task<PlanRecoveryDto> RequestCalculationAsync(
        PlanScenarioQuery scenario,
        CancellationToken cancellationToken = default)
    {
        RequireHousehold();
        return await _planService.GetRecoveryAsync(scenario, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PlanningScenarioComparisonDto> CompareScenariosAsync(
        PlanScenarioQuery baseline,
        PlanScenarioQuery alternative,
        CancellationToken cancellationToken = default)
    {
        RequireHousehold();
        var baselineResult = await _planService.GetRecoveryAsync(baseline, cancellationToken);
        var alternativeResult = await _planService.GetRecoveryAsync(alternative, cancellationToken);
        return new PlanningScenarioComparisonDto
        {
            Applied = false,
            Baseline = baselineResult,
            Alternative = alternativeResult
        };
    }

    /// <inheritdoc />
    public async Task<RestructuringComparisonDto> CompareRestructuringAsync(
        CompareRestructuringDto dto,
        CancellationToken cancellationToken = default)
    {
        RequireHousehold();
        return await _restructuringService.CompareAsync(dto, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PlanChangeDraftDto> DraftPlanChangeAsync(
        PlanScenarioQuery scenario,
        PayoffRolloverKind kind,
        CancellationToken cancellationToken = default)
    {
        RequireHousehold();
        var plan = await _savedPlanService.GetPreviewAsync(scenario, kind, cancellationToken);
        return new PlanChangeDraftDto { Applied = false, Plan = plan };
    }

    #region Private Methods

    /// <summary>
    /// Requires a household before any tool reads or checks a proposal.
    /// </summary>
    private void RequireHousehold()
    {
        _householdScope.RequireHouseholdId();
    }

    /// <summary>
    /// Turns a rejected tool check into a bad request.
    /// </summary>
    private static void Reject(string? message)
    {
        if (message is not null)
        {
            throw new BadRequestException(message);
        }
    }

    /// <summary>
    /// Loads the one summary the tool was asked for.
    /// Each section comes from the service the screen uses. The other sections stay null.
    /// </summary>
    private async Task<PlanningSummaryDto> LoadSummaryAsync(
        PlanningSummaryKind kind,
        CancellationToken cancellationToken)
    {
        return kind switch
        {
            PlanningSummaryKind.Cash => new PlanningSummaryDto
            {
                Kind = kind,
                Cash = await _accountsService.GetCashPositionAsync(cancellationToken)
            },
            PlanningSummaryKind.Debts => new PlanningSummaryDto
            {
                Kind = kind,
                Debts = await _debtsService.GetSummaryAsync(cancellationToken)
            },
            PlanningSummaryKind.Income => new PlanningSummaryDto
            {
                Kind = kind,
                Income = await _incomeSourcesService.GetIncomeSourcesAsync(cancellationToken)
            },
            PlanningSummaryKind.Bills => new PlanningSummaryDto
            {
                Kind = kind,
                Bills = await _obligationsService.GetObligationsAsync(cancellationToken)
            },
            PlanningSummaryKind.Savings => new PlanningSummaryDto
            {
                Kind = kind,
                Savings = await _savingsGoalsService.GetGoalsAsync(cancellationToken)
            },
            PlanningSummaryKind.Living => new PlanningSummaryDto
            {
                Kind = kind,
                Living = await _livingService.GetAsync(cancellationToken)
            },
            PlanningSummaryKind.Allocation => new PlanningSummaryDto
            {
                Kind = kind,
                Allocation = await _allocationService.GetAsync(cancellationToken)
            },
            PlanningSummaryKind.Plan => new PlanningSummaryDto
            {
                Kind = kind,
                Plan = await _savedPlanService.ListAsync(cancellationToken)
            },
            _ => throw new BadRequestException("Choose a summary.")
        };
    }

    /// <summary>
    /// Checks a proposed income source the same way saving one does.
    /// The typical amount stays one payment.
    /// </summary>
    private static IncomeSourceDraft RequireIncome(UpsertIncomeSourceDto dto)
    {
        var raises = (dto.Raises ?? [])
            .Select(raise => new IncomeRaiseDraft(raise.EffectiveDate, raise.TakeHomeAmount))
            .ToList();

        if (!IncomeSourceRules.TryNormalize(
                dto.Name,
                dto.TakeHomeAmount,
                dto.Cadence,
                dto.NextPaymentDate,
                dto.ContributorId,
                dto.Reliability,
                out var draft,
                out var error,
                dto.LowTakeHomeAmount,
                dto.StrongTakeHomeAmount,
                raises,
                dto.GrossPayAmount))
        {
            throw new BadRequestException(error);
        }

        return draft;
    }

    /// <summary>
    /// Checks a proposed bill the same way saving one does.
    /// The amount stays one payment.
    /// </summary>
    private static ObligationDraft RequireBill(UpsertObligationDto dto)
    {
        if (!ObligationRules.TryNormalize(
                dto.Name,
                dto.Amount,
                dto.Cadence,
                dto.NextDueDate,
                dto.AccountId,
                dto.Flexibility,
                out var draft,
                out var error))
        {
            throw new BadRequestException(error);
        }

        return draft;
    }

    /// <summary>
    /// Checks a proposed debt the same way saving one does.
    /// A blank term stays unknown.
    /// </summary>
    private static DebtDraft RequireDebt(UpsertDebtDto dto)
    {
        if (!DebtRules.TryNormalize(
                dto.Name,
                dto.Kind,
                dto.AccountId,
                dto.Balance,
                dto.BalanceAsOf,
                dto.Apr,
                dto.MinimumPayment,
                dto.NextDueDate,
                dto.CreditLimit,
                dto.RemainingTermMonths,
                dto.PromotionalApr,
                dto.PromotionalEndsOn,
                out var draft,
                out var error))
        {
            throw new BadRequestException(error);
        }

        return draft;
    }

    /// <summary>
    /// Checks a proposed savings row the same way saving one does.
    /// </summary>
    private static SavingsGoalDraft RequireSavings(UpsertSavingsGoalDto dto)
    {
        if (!SavingsGoalRules.TryNormalize(
                dto.Kind,
                dto.Name,
                dto.TargetAmount,
                dto.TargetDate,
                dto.ReservedAmount,
                dto.AccountId,
                dto.UseAccountBalance,
                dto.MonthlyAmount,
                dto.ReadyDay,
                dto.FloorAmount,
                out var draft,
                out var error))
        {
            throw new BadRequestException(error);
        }

        return draft;
    }

    /// <summary>
    /// Checks a proposed contribution the same way saving one does.
    /// Null stays unset.
    /// </summary>
    private static decimal? RequireLivingContribution(UpsertLivingContributionDto dto)
    {
        if (!LivingContributionRules.TryNormalize(dto.MonthlyAmount, out var stored, out var error))
        {
            throw new BadRequestException(error);
        }

        return stored;
    }

    /// <summary>
    /// Checks a proposed allocation the same way saving one does.
    /// Debt and savings together have to leave room to stay available.
    /// </summary>
    private static AllocationPreferenceDraft RequireAllocation(UpsertAllocationPreferenceDto dto)
    {
        if (!AllocationPreferenceRules.TryNormalize(
                dto.VariableIncomeDebtPercent,
                dto.VariableIncomeSavingsPercent,
                dto.WindfallDebtPercent,
                dto.WindfallSavingsPercent,
                out var draft,
                out var error)
            || draft is null)
        {
            throw new BadRequestException(error);
        }

        return draft;
    }

    /// <summary>
    /// Rejects an account that is not in this household.
    /// A missing account means the fact is not tied to one. An archived account may still be named.
    /// </summary>
    private async Task RequireHouseholdAccountAsync(
        Guid? accountId,
        CancellationToken cancellationToken)
    {
        if (accountId is not Guid id)
        {
            return;
        }

        var accounts = await _accountsService.GetAccountsAsync(
            includeArchived: true,
            cancellationToken);
        if (accounts.All(account => account.Id != id))
        {
            throw new BadRequestException("Choose an account from this household.");
        }
    }

    /// <summary>
    /// Rejects a contributor that is not in this household.
    /// A missing contributor means the income is not assigned.
    /// </summary>
    private async Task RequireHouseholdContributorAsync(
        Guid? contributorId,
        CancellationToken cancellationToken)
    {
        if (contributorId is not Guid id)
        {
            return;
        }

        var profile = await _financialProfileService.GetAsync(cancellationToken);
        if (profile.Contributors.All(contributor => contributor.Id != id))
        {
            throw new BadRequestException("Choose a contributor from this household.");
        }
    }

    /// <summary>
    /// Rejects a contribution for a person this household does not have.
    /// </summary>
    private async Task RequireContributorExistsAsync(
        Guid contributorId,
        CancellationToken cancellationToken)
    {
        var profile = await _financialProfileService.GetAsync(cancellationToken);
        if (profile.Contributors.All(contributor => contributor.Id != contributorId))
        {
            throw new NotFoundException("That contributor was not found.");
        }
    }

    /// <summary>
    /// Rejects an account a new savings row cannot follow.
    /// The account has to be cash in this household. An account another goal already follows is rejected.
    /// </summary>
    private async Task RequireSavingsAccountAsync(
        Guid? accountId,
        CancellationToken cancellationToken)
    {
        if (accountId is not Guid id)
        {
            return;
        }

        var accounts = await _savingsGoalsService.GetAccountsAsync(cancellationToken);
        var match = accounts.FirstOrDefault(account => account.Id == id);
        if (match is null)
        {
            throw new BadRequestException("Choose a cash account in this household.");
        }

        if (match.FollowedByGoalId is not null)
        {
            throw new BadRequestException("That account is already followed by another goal.");
        }
    }

    #endregion
}
